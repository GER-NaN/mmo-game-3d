namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using MmoGame3d.Data;
using MmoGame3d.Data.Accounts;
using MmoGame3d.Data.Maps;
using MmoGame3d.Data.Players;
using MmoGame3d.Data.Progress;
using MmoGame3d.Data.Social;
using MmoGame3d.Data.Town;
using MmoGame3d.Interact;
using MmoGame3d.Items;
using MmoGame3d.Networking;
using MmoGame3d.Players;
using MmoGame3d.Rules;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Players;
using MmoGame3d.Rules.Shops;
using MmoGame3d.Rules.Skills;
using MmoGame3d.Rules.Social;
using MmoGame3d.Rules.Time;
using MmoGame3d.Town;
using MmoGame3d.Rules.World;
using MmoGame3d.Zones;

/// <summary>
/// The server's side of the game: who is connected, logging in and out, putting bodies
/// in the world, what players carry, and writing players down. Game state is only
/// touched on this thread; the database runs on the persistence worker and answers
/// through its completions. The parts with a life of their own live in helpers: who
/// sees what (VisibilityGate) and the items on the ground (GroundItems).
/// </summary>
public partial class ServerGame : Node
{
    // How often players online are saved. Ending a session saves too, so this only
    // bounds what a server crash can lose.
    private const double SaveIntervalSeconds = 30;

    // How often every client is told the time. They count the seconds between.
    private const double ClockIntervalSeconds = 10;

    // Party members this close to whoever walks through a door go through with them.
    private const float PartyFollowDistance = 10f;

    private static readonly TimeSpan ShutdownDrain = TimeSpan.FromSeconds(10);
    private static readonly PackedScene PlayerScene = GD.Load<PackedScene>("res://game/player/Player.tscn");

    private readonly Dictionary<long, Session> _sessions = new Dictionary<long, Session>();

    private LaunchOptions _options = null!;
    private Network _network = null!;
    private World _world = null!;
    private PersistenceWorker _worker = null!;
    private AccountStore _accounts = null!;
    private PlayerStore _players = null!;
    private DiscoveryStore _discoveries = null!;
    private ContactStore _contacts = null!;
    private ProgressStore _progressStore = null!;
    private StopSignals? _stopSignals;
    private VisibilityGate _gate = null!;
    private GroundItems _groundItems = null!;
    private ServerChat _chat = null!;
    private ServerParties _parties = null!;
    private ServerTerminals _terminals = null!;
    private ServerInteractions _interactions = null!;
    private ServerShops _shops = null!;
    private ServerEquipment _equipment = null!;
    private ServerTown _town = null!;
    private ServerChests _chests = null!;
    private ServerRides _rides = null!;
    private ServerMaps _maps = null!;
    private ServerSocial _social = null!;
    private ServerProgress _progress = null!;
    private ServerFixables _fixables = null!;
    private ServerHacking _hacking = null!;
    private ServerCollege _college = null!;
    private WhoisStore _whoisStore = null!;
    private ServerWhois _whois = null!;
    private ServerDrones _drones = null!;
    private WorldClock _clock = null!;
    private bool _stocked;
    private double _sinceSave;
    private double _sinceClock;
    private double _sinceStats;
    private ulong _physicsFramesAtStats;
    private ENetMultiplayerPeer? _peer;
    private ServerDiagnostics? _diagnostics;
    private NativePacketLog? _packetLog;

    public void Start(LaunchOptions options, Networks networks, World world)
    {
        Network network = networks.Session;
        PartyNetwork partyNetwork = networks.Party;
        _options = options;
        _network = network;
        _world = world;
        StartDiagnostics(networks);

        for (int i = 0; i < ZoneIds.All.Length; i++)
        {
            Zone zone = _world.LoadZone(ZoneIds.All[i], new Vector3(i * World.ZoneSpacing, 0f, 0f));

            foreach (Node node in zone.GetNode("Doors").GetChildren())
            {
                Door? door = node as Door;

                if (door != null)
                {
                    door.Entered += OnDoorEntered;
                }
            }
        }

        // Migrations run before the server listens: a schema that cannot be brought up
        // to date is a server that must not take players.
        Database database = new Database(options.DatabaseConnection);

        try
        {
            IReadOnlyList<string> applied = MigrationRunner.Run(database);
            GD.Print("Database ready; migrations applied: " + (applied.Count == 0 ? "none" : string.Join(", ", applied)));
        }
        catch (Exception e)
        {
            GD.PrintErr("Could not prepare the database: " + e.Message);
            GetTree().Quit(1);
            return;
        }

        _accounts = new AccountStore(database);
        _players = new PlayerStore(database);
        _discoveries = new DiscoveryStore(database);
        _worker = new PersistenceWorker();
        _maps = new ServerMaps(world, network, _worker, _discoveries, () => _sessions.Values);
        _contacts = new ContactStore(database);
        _social = new ServerSocial(networks.Social, network, _worker, _contacts, () => _sessions.Values, FindSession);
        _progressStore = new ProgressStore(database);
        _whoisStore = new WhoisStore(database);
        _progress = new ServerProgress(networks.Progress, network, () => _sessions.Values);
        _stopSignals = new StopSignals(options.Port);
        _clock = new WorldClock(options.TimeZone, options.TimeOffsetHours);
        TimeSpan worldTime = TimeSpan.FromSeconds(_clock.SecondsOfDay(DateTime.UtcNow));
        GD.Print("World time " + worldTime.ToString(@"hh\:mm") + " (" + options.TimeZone + (options.TimeOffsetHours != 0 ? ", shifted " + options.TimeOffsetHours + " h" : "") + ")");
        _gate = new VisibilityGate(CanSee);
        _groundItems = new GroundItems(_gate, OnItemPickedUp);
        _chat = new ServerChat(network, () => _sessions.Values);
        _parties = new ServerParties(partyNetwork, network, _chat, () => _sessions.Values);
        _terminals = new ServerTerminals(networks.Terminal, network, () => _sessions.Values);
        ServerIntents intents = new ServerIntents(network);
        _shops = new ServerShops(networks.Shop, network, intents, SendInventory);
        ServerHandover handover = new ServerHandover(intents, network, _groundItems, world, FindSession, SendInventory);
        ServerRecycling recycling = new ServerRecycling(networks.Items, network, intents, SendInventory);
        networks.Items.RecycleOneRequested += (peer, intent, type, tier) => WithSession(peer, session => recycling.RecycleOne(session, intent, type, tier));
        networks.Items.RecycleThingRequested += (peer, intent, id) => WithSession(peer, session => recycling.RecycleThing(session, intent, id));
        networks.Items.DropRequested += (peer, intent, type, tier, quantity) => WithSession(peer, session => handover.Drop(session, intent, type, tier, quantity));
        networks.Items.GiveRequested += (peer, intent, target, type, tier, quantity, dollars) => WithSession(peer, session => handover.Give(session, intent, target, type, tier, quantity, dollars));
        _equipment = new ServerEquipment(networks.Items, network, _terminals, () => _sessions.Values, SendInventory);
        _equipment.WorkDone += (session, withPack) =>
        {
            _progress.Award(session, SkillId.Workbench, SkillAwards.WorkbenchPerJob);

            if (withPack)
            {
                _progress.AwardCareer(session, SkillAwards.RepairPackPerJob);
            }
        };
        networks.Items.RepairPackRequested += peer => WithSession(peer, session => _equipment.OpenRepairPack(session));
        _chests = new ServerChests(network, SendInventory);
        _interactions = new ServerInteractions(world, network, _terminals, _shops, _equipment, _chests);
        _interactions.Recycling = recycling;

        TownState townState = _world.GetZone(ZoneIds.Town)!.GetNode<TownState>("TownState");
        _gate.Watch(townState.Synchronizer, ZoneIds.Town);
        _town = new ServerTown(townState, new TownStore(database), _worker, network, networks.Terminal, _chat, _clock, () => _sessions.Values, SendInventory);
        _town.Load();
        _town.Repaired += session =>
        {
            _progress.Award(session, SkillId.ElectricalRepair, SkillAwards.ElectricalRepairPerBox);
            _progress.MissionDone(session);
        };
        _interactions.Town = _town;
        _rides = new ServerRides(world, _gate, network, _parties, () => _sessions.Values, Travel);
        _interactions.Rides = _rides;

        List<Fixable> fixables = new List<Fixable>();

        foreach (string zoneId in ZoneIds.All)
        {
            foreach (Node node in _world.GetZone(zoneId)!.GetNode(Interactable.ParentName).GetChildren())
            {
                if (node is Fixable fixable)
                {
                    fixables.Add(fixable);
                }
            }
        }

        _fixables = new ServerFixables(fixables, network, _progress);
        _interactions.Fixables = _fixables;
        _hacking = new ServerHacking(networks.Terminal, network, _terminals, _progress);
        _college = new ServerCollege(networks.Progress, network, _progress);
        _whois = new ServerWhois(networks.Social, network, _worker, _whoisStore, _terminals, () => _sessions.Values);
        _whois.HasJob = _town.HasJob;
        _drones = new ServerDrones(_world.GetZone(ZoneIds.Town)!, _gate, network, () => _sessions.Values);
        network.EmpRequested += peer => WithSession(peer, session => _drones.Fire(session));
        _drones.Hurt += HurtPlayer;
        networks.Social.WhoisSearchRequested += (peer, text) => WithSession(peer, session => _whois.Search(session, text));
        networks.Social.WhoisOpenRequested += (peer, id) => WithSession(peer, session => _whois.Open(session, id));
        networks.Social.WhoisPropsRequested += (peer, id) => WithSession(peer, session => _whois.ToggleProps(session, id));
        networks.Social.WhoisEditRequested += (peer, plan, skills, location) => WithSession(peer, session => _whois.Edit(session, plan, skills, location));
        networks.Social.BefriendIdRequested += (peer, id) => WithSession(peer, session => _social.BefriendId(session, id));
        _interactions.College = _college;
        networks.Progress.TakeClassRequested += peer => WithSession(peer, session => _college.TakeClass(session));
        networks.Progress.EnrollRequested += (peer, career) => WithSession(peer, session => _college.Enroll(session, career));
        networks.Progress.RankUpRequested += peer => WithSession(peer, session => _college.RankUp(session));
        _progress.CareerChanged += session =>
        {
            if (session.Body != null)
            {
                session.Body.CareerTitle = CareerCatalog.Title(session.Progress.Career.Career, session.Progress.Career.Rank);
            }
        };
        networks.Terminal.CrackStartRequested += peer => WithSession(peer, session => _hacking.Start(session));
        networks.Terminal.CrackGuessRequested += (peer, guess) => WithSession(peer, session => _hacking.Guess(session, guess));
        _terminals.Opened += _town.SendTown;

        // Things standing in the zones sync their state (a terminal in use) only to the
        // players in that zone, like everything else.
        foreach (string zoneId in ZoneIds.All)
        {
            foreach (Node node in _world.GetZone(zoneId)!.GetNode(Interactable.ParentName).GetChildren())
            {
                MultiplayerSynchronizer? synchronizer = (node as Interactable)?.Synchronizer;

                if (synchronizer != null)
                {
                    _gate.Watch(synchronizer, zoneId);
                }
            }
        }

        ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
        Error error = peer.CreateServer(options.Port, options.MaxPlayers);

        if (error != Error.Ok)
        {
            GD.PrintErr("Could not listen on port " + options.Port + ": " + error);
            GetTree().Quit(1);
            return;
        }

        _peer = peer;

        // The packet log wraps the ENet peer in native code: through a C# wrapper the
        // engine's per-synchronizer calls cost more than the rest of the server (see
        // docs/engineering/diagnostics.md).
        if (_diagnostics != null && _options.LogPackets)
        {
            _packetLog = NativePacketLog.Wrap(peer, _diagnostics);
        }

        Multiplayer.MultiplayerPeer = _packetLog != null ? _packetLog.Peer : peer;

        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
        _network.HelloReceived += OnHello;
        _network.CreateCharacterRequested += OnCreateCharacter;
        _network.LoginRequested += OnLoginRequested;
        _network.SetLookRequested += (peer, look) => WithSession(peer, session => OnSetLook(session, look));
        _network.WorldReadyReceived += OnWorldReady;
        _network.ChatRequested += OnChatRequested;
        _network.DirectRequested += (peer, target, text) => WithSession(peer, session => _chat.Direct(session, target, text));
        _network.WalkRequested += (peer, direction, heading) => WithSession(peer, session => session.Body?.ApplyWalk(direction, heading));
        _network.StopRequested += (peer, heading) => WithSession(peer, session => session.Body?.ApplyStop(heading));
        _network.JumpRequested += peer => WithSession(peer, session => session.Body?.ApplyJump());
        _network.EmoteRequested += (peer, id) => WithSession(peer, session =>
        {
            // Only emotes may be asked for; the action gestures are the server's to show.
            if (Gestures.Find(id)?.IsEmote == true && session.Body != null && !session.Body.IsOnline)
            {
                session.Body.Show(id);
            }
        });
        partyNetwork.InviteRequested += (peer, target) => WithSession(peer, session => _parties.Invite(session, FindSession(target)));
        partyNetwork.ResponseReceived += (peer, inviter, accept) => WithSession(peer, session => _parties.Respond(session, inviter, accept));
        partyNetwork.LeaveRequested += peer => WithSession(peer, session => _parties.Leave(session));
        partyNetwork.ChatRequested += (peer, text) => WithSession(peer, session => _parties.Chat(session, text));
        _network.InteractRequested += (peer, name) => WithSession(peer, session => _interactions.Use(session, name));
        networks.Terminal.LeaveRequested += peer => WithSession(peer, session => _terminals.Leave(session));
        networks.Terminal.TakeJobRequested += (peer, job) => WithSession(peer, session => _town.TakeJob(session));
        networks.Shop.BuyRequested += (peer, intent, shop, offer) => WithSession(peer, session => _shops.Buy(session, intent, shop, offer));
        networks.Items.EquipRequested += (peer, id) => WithSession(peer, session => _equipment.Equip(session, id));
        networks.Items.UnequipRequested += (peer, id) => WithSession(peer, session => _equipment.Unequip(session, id));
        networks.Items.PhoneRequested += peer => WithSession(peer, session => _equipment.UsePhone(session));
        networks.Items.RemoveBatteryRequested += (peer, id) => WithSession(peer, session => _equipment.RemoveBattery(session, id));
        networks.Items.InsertBatteryRequested += (peer, id) => WithSession(peer, session => _equipment.InsertBattery(session, id));
        networks.Social.BefriendRequested += (peer, target) => WithSession(peer, session => _social.Befriend(session, target));
        networks.Social.IgnoreRequested += (peer, target) => WithSession(peer, session => _social.Ignore(session, target));
        networks.Social.RemoveRequested += (peer, id) => WithSession(peer, session => _social.Remove(session, id));

        GD.Print("Server listening on port " + options.Port + " for up to " + options.MaxPlayers + " players");
    }

    public override void _Process(double delta)
    {
        if (_worker == null)
        {
            return;
        }

        _worker.RunCompletions();
        _parties.Tick(delta);
        _terminals.Tick(delta);
        _equipment.Tick(delta);
        _town.Tick(delta);
        _chests.Tick();
        _rides.Tick(delta);
        _maps.Tick(delta);
        _progress.Tick(delta);
        _fixables.Tick(delta);
        _drones.Tick(delta);
        RegenerateHealth(delta);
        _packetLog?.Drain();
        _diagnostics?.Tick(delta);

        if (_stopSignals != null && _stopSignals.StopRequested())
        {
            GD.Print("Stopping: saving players online");
            GetTree().Quit();
            return;
        }

        _sinceClock += delta;

        if (_sinceClock >= ClockIntervalSeconds)
        {
            _sinceClock = 0;

            foreach (Session session in _sessions.Values)
            {
                if (session.State == SessionState.InWorld)
                {
                    SendClock(session);
                }
            }
        }

        if (_options.StatsEverySeconds > 0)
        {
            _sinceStats += delta;

            if (_sinceStats >= _options.StatsEverySeconds)
            {
                PrintStats(_sinceStats);
                _sinceStats = 0;
            }
        }

        _sinceSave += delta;

        if (_sinceSave >= SaveIntervalSeconds)
        {
            _sinceSave = 0;
            SaveEveryone();
        }
    }

    // Physics queries are only sure once the space has stepped, so zones are stocked on
    // the first physics frame rather than at start.
    public override void _PhysicsProcess(double delta)
    {
        if (_groundItems == null)
        {
            return;
        }

        if (!_stocked)
        {
            _stocked = true;

            foreach (string zoneId in ZoneIds.All)
            {
                _groundItems.Stock(_world.GetZone(zoneId)!);
            }
        }

        _groundItems.Tick(delta);
    }

    public override void _ExitTree()
    {
        if (_worker == null)
        {
            return;
        }

        SaveEveryone();

        if (!_worker.Stop(ShutdownDrain))
        {
            GD.PrintErr("Shutdown: the last saves did not finish in time");
        }

        _worker.Dispose();

        if (_stopSignals != null)
        {
            _stopSignals.MarkStopped();
            _stopSignals.Dispose();
        }

        GD.Print("Server stopped");
        _diagnostics?.Dispose();
    }

    private void StartDiagnostics(Networks networks)
    {
        string? path = _options.DiagnosticsPath;

        if (path == "off")
        {
            return;
        }

        if (path == null)
        {
            path = ProjectSettings.GlobalizePath("user://diagnostics/server-" + _options.Port + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".jsonl");
        }

        _diagnostics = new ServerDiagnostics(path);
        networks.SetLog(_diagnostics);
        GD.Print("Diagnostics go to " + _diagnostics.FilePath);
    }

    private void OnPeerConnected(long peer)
    {
        _sessions[peer] = new Session(peer);
        GD.Print("Peer " + peer + " connected");
    }

    private void OnPeerDisconnected(long peer)
    {
        if (!_sessions.TryGetValue(peer, out Session? session))
        {
            return;
        }

        _sessions.Remove(peer);
        _chat.Forget(peer);
        _diagnostics?.Forget(peer);

        if (session.HasEnteredWorld)
        {
            _terminals.Disconnected(session);
            Save(session);
            session.Body?.QueueFree();
            _parties.LeftWorld(session);
            _social.WentOffline(session);
            _progress.Forget(session);
            _hacking.Forget(session);
            _drones.Forget(session);
            _chat.Announce(session.Record!.DisplayName + " left.");
        }

        GD.Print("Peer " + peer + " left" + (session.Record != null ? " (" + session.Record.DisplayName + ")" : ""));
    }

    // Who the account is: the license key, checked and made an account if new. The
    // answer is the account's characters.
    private void OnHello(long peer, int protocol, string licenseKeyText)
    {
        if (!_sessions.TryGetValue(peer, out Session? session) || session.State != SessionState.Connected)
        {
            return;
        }

        if (protocol != GameVersion.Protocol)
        {
            Refuse(peer, "This game is version " + protocol + " and the server is version " + GameVersion.Protocol + ". Update the game.");
            return;
        }

        Guid licenseKey;

        if (!Guid.TryParse(licenseKeyText, out licenseKey))
        {
            Refuse(peer, "The license key is not valid.");
            return;
        }

        session.State = SessionState.LoggingIn;
        _worker.Enqueue(
            () => _accounts.GetOrCreate(licenseKey),
            accountId =>
            {
                session.AccountId = accountId;
                session.State = SessionState.Choosing;
                SendCharacters(session, "");
            },
            e =>
            {
                GD.PrintErr("Hello from peer " + peer + " failed: " + e.Message);
                Refuse(peer, "The server could not find your account. Try again.");
            });
    }

    private void SendCharacters(Session session, string message)
    {
        Guid accountId = session.AccountId!.Value;
        long peer = session.PeerId;
        _worker.Enqueue(
            () => _players.ListForAccount(accountId),
            list =>
            {
                string[] ids = new string[list.Count];
                string[] names = new string[list.Count];
                string[] looks = new string[list.Count];
                int[] levels = new int[list.Count];
                string[] titles = new string[list.Count];

                for (int i = 0; i < list.Count; i++)
                {
                    CharacterSummary character = list[i];
                    CareerId? career = character.Career.HasValue && CareerCatalog.Find(character.Career.Value) != null ? (CareerId)character.Career.Value : null;
                    ids[i] = character.PlayerId.ToString();
                    names[i] = character.DisplayName;
                    looks[i] = Appearance.Normalize(character.Look);
                    levels[i] = PlayerLevel.For((long)(character.SecondsPlayed / 60), character.SkillXp, character.CareerXp, character.Missions);
                    titles[i] = CareerCatalog.Title(career, (CareerRank)character.CareerRank);
                }

                _network.SendCharacters(peer, ids, names, looks, levels, titles, message);
            },
            e => GD.PrintErr("Listing characters failed: " + e.Message));
    }

    // A new character in the first free slot: the name checked, the look normalized, and
    // the starter kit a new player gets.
    private void OnCreateCharacter(long peer, string displayName, string look)
    {
        if (!_sessions.TryGetValue(peer, out Session? session) || session.State != SessionState.Choosing)
        {
            return;
        }

        string? nameProblem = DisplayName.Problem(displayName);

        if (nameProblem != null)
        {
            SendCharacters(session, nameProblem);
            return;
        }

        Zone start = _world.GetZone(ZoneIds.Start)!;
        Vector3 spawn = start.SpawnPoint;
        PlayerRecord newPlayer = new PlayerRecord
        {
            PlayerId = Guid.NewGuid(),
            AccountId = session.AccountId!.Value,
            DisplayName = displayName.Trim(),
            Zone = ZoneIds.Start,
            PositionX = spawn.X,
            PositionY = spawn.Y,
            PositionZ = spawn.Z,
            Dollars = Shops.StartingDollars,
            Look = Appearance.Normalize(look),
        };
        newPlayer.Instances.AddRange(Belongings.StarterKit());

        _worker.Enqueue(
            () =>
            {
                int slot = Characters.FreeSlot(_players.ListForAccount(newPlayer.AccountId).ConvertAll(c => c.Slot));

                if (slot < 0 || !_players.Create(newPlayer, slot))
                {
                    return "All your character slots are taken.";
                }

                _players.Save(newPlayer);
                return "";
            },
            problem =>
            {
                GD.Print(problem.Length == 0 ? "Peer " + peer + " made a character, " + newPlayer.DisplayName : "Peer " + peer + ": " + problem);
                SendCharacters(session, problem);
            },
            e =>
            {
                GD.PrintErr("Creating a character for peer " + peer + " failed: " + e.Message);
                SendCharacters(session, "The server could not make that character. Try again.");
            });
    }

    // Playing one of the account's own characters.
    private void OnLoginRequested(long peer, string playerIdText)
    {
        Guid playerId;

        if (!_sessions.TryGetValue(peer, out Session? session) || session.State != SessionState.Choosing || !Guid.TryParse(playerIdText, out playerId))
        {
            return;
        }

        Guid accountId = session.AccountId!.Value;
        session.State = SessionState.LoggingIn;

        _worker.Enqueue(
            () =>
            {
                PlayerRecord? record = _players.Load(playerId, accountId);

                if (record != null)
                {
                    record.Discovered = _discoveries.Load(record.PlayerId);
                    record.Contacts = _contacts.Load(record.PlayerId);
                    record.Progress = _progressStore.Load(record.PlayerId);
                    record.Page = _whoisStore.LoadSettings(record.PlayerId);
                }

                return record;
            },
            record =>
            {
                if (record == null)
                {
                    session.State = SessionState.Choosing;
                    SendCharacters(session, "That character is not yours.");
                    return;
                }

                OnPlayerLoaded(peer, record);
            },
            e =>
            {
                GD.PrintErr("Login for peer " + peer + " failed: " + e.Message);
                Refuse(peer, "The server could not load your player. Try again.");
            });
    }

    // The wardrobe: a new look, checked and normalized, seen by everyone at once.
    private void OnSetLook(Session session, string look)
    {
        string normalized = Appearance.Normalize(look);
        session.Record!.Look = normalized;

        if (session.Body != null)
        {
            session.Body.Look = normalized;
        }

        Guid playerId = session.Record.PlayerId;
        _worker.Enqueue(() => _players.SaveLook(playerId, normalized), e => GD.PrintErr("Saving a look failed: " + e.Message));
    }

    private void OnPlayerLoaded(long peer, PlayerRecord record)
    {
        // The client may have gone while the lookup ran.
        if (!_sessions.TryGetValue(peer, out Session? session))
        {
            return;
        }

        foreach (Session other in _sessions.Values)
        {
            if (other != session && other.Record != null && other.Record.AccountId == record.AccountId)
            {
                Refuse(peer, "This player is already online.");
                return;
            }
        }

        // A zone removed since the player was saved would lock them out; they start over.
        if (_world.GetZone(record.Zone) == null)
        {
            Zone start = _world.GetZone(ZoneIds.Start)!;
            record.Zone = ZoneIds.Start;
            record.PositionX = start.SpawnPoint.X;
            record.PositionY = start.SpawnPoint.Y;
            record.PositionZ = start.SpawnPoint.Z;
        }

        session.Record = record;
        session.Dollars = record.Dollars;
        session.Contacts = record.Contacts;
        session.Progress = record.Progress;
        session.Page = record.Page;
        _diagnostics?.Tag(peer, "player.name", record.DisplayName);
        _diagnostics?.Tag(peer, "player.id", record.PlayerId.ToString());
        session.Inventory = new Inventory();

        foreach (ItemStack stack in record.Stacks)
        {
            session.Inventory.Add(stack.Type, stack.Tier, stack.Quantity);
        }

        // A new player arrives as if just out of the Training Grounds: a phone at 10%, in
        // the bag rather than equipped, and pocket change.
        if (record.Created)
        {
            record.Instances.AddRange(Belongings.StarterKit());
        }

        session.Instances = record.Instances;

        session.State = SessionState.Accepted;
        _network.SendLoginAccepted(peer, record.Zone, record.DisplayName);
        GD.Print("Peer " + peer + " logged in as " + record.DisplayName + " (player " + record.PlayerId + ")");
    }

    // The client has its zone loaded, so the body can be spawned there without the
    // spawn arriving before the node it goes under.
    private void OnWorldReady(long peer)
    {
        if (!_sessions.TryGetValue(peer, out Session? session) || session.State != SessionState.Accepted || session.Record == null)
        {
            return;
        }

        PlayerRecord record = session.Record;
        Zone zone = _world.GetZone(record.Zone)!;

        Player body = PlayerScene.Instantiate<Player>();
        body.Name = peer.ToString();
        body.DisplayName = record.DisplayName;
        body.PlayerIdText = record.PlayerId.ToString();
        body.Look = Appearance.Normalize(record.Look);
        body.CareerTitle = CareerCatalog.Title(session.Progress.Career.Career, session.Progress.Career.Rank);
        body.Health = session.Health;
        body.Position = SpaceQueries.FreeSpotNear(zone, new Vector3(record.PositionX, record.PositionY, record.PositionZ));
        body.Rotation = new Vector3(0f, record.Yaw, 0f);
        body.RespawnPoint = zone.SpawnPoint;

        _diagnostics?.Tag(peer, "player.zone", zone.ZoneId);
        _gate.Watch(body.Synchronizer, zone.ZoneId);
        zone.Players.AddChild(body, true);
        session.Body = body;
        session.State = SessionState.InWorld;

        // The newcomer can now see the zone: everything there asks its filter again, so
        // the players and items already standing there spawn on this client.
        _gate.Refresh(zone.ZoneId);
        SendInventory(session);
        SendClock(session);
        _maps.Send(session);
        _progress.Send(session);
        _parties.EnteredWorld(session);

        if (!session.HasEnteredWorld)
        {
            session.HasEnteredWorld = true;
            _chat.Announce(record.DisplayName + " joined.");
            _social.CameOnline(session);
        }
        else
        {
            _social.Arrived(session);
        }
    }

    // Something hurt a player. At 0 they faint and helpful strangers carry them back to
    // the town's spawn (world.md 8), with their HP back; nothing is lost yet, since what
    // fainting costs is still open.
    private void HurtPlayer(Session session, int amount)
    {
        session.Health = Rules.Players.Health.Hurt(session.Health, amount);
        session.SinceHurt = 0;

        if (session.Body != null)
        {
            session.Body.Health = session.Health;
        }

        if (!Rules.Players.Health.HasFainted(session.Health))
        {
            _network.SendNotice(session.PeerId, "A drone zaps you. HP " + session.Health + ".");
            return;
        }

        GD.Print(session.Record!.DisplayName + " fainted");
        session.Health = Rules.Players.Health.Max;
        Zone town = _world.GetZone(ZoneIds.Town)!;
        _network.SendNotice(session.PeerId, "You fainted. Helpful strangers carried you back to town.");
        Travel(new List<Session> { session }, town, town.GetNode<Node3D>("Spawn"), "");
    }

    private void RegenerateHealth(double delta)
    {
        foreach (Session session in _sessions.Values)
        {
            if (session.State != SessionState.InWorld || session.Health >= Rules.Players.Health.Max)
            {
                continue;
            }

            double before = session.SinceHurt;
            session.SinceHurt += delta;

            // Whole seconds past the wait, a few HP each.
            int ticks = (int)Math.Floor(session.SinceHurt - Rules.Players.Health.RegenAfterSeconds) - (int)Math.Floor(before - Rules.Players.Health.RegenAfterSeconds);

            if (session.SinceHurt >= Rules.Players.Health.RegenAfterSeconds && ticks > 0)
            {
                session.Health = Rules.Players.Health.Heal(session.Health, ticks * Rules.Players.Health.RegenPerSecond);

                if (session.Body != null)
                {
                    session.Body.Health = session.Health;
                }
            }
        }
    }

    // Through a door. The party goes together (first-playable): members in the same zone
    // and close by go through with whoever walked in, and arrive around them.
    private void OnDoorEntered(Door door, Player player)
    {
        Session? session = FindSession(player.OwnerPeerId);
        Zone? target = _world.GetZone(door.TargetZone);
        Node3D? arrival = target?.Arrival(door.TargetArrival);

        if (target == null || arrival == null)
        {
            GD.PrintErr("Door " + door.GetPath() + " leads to " + door.TargetZone + "/" + door.TargetArrival + ", which is not there");
            return;
        }

        if (session == null || session.State != SessionState.InWorld || session.Record == null || session.Body == null)
        {
            return;
        }

        Vector3 at = session.Body.GlobalPosition;
        List<Session> travellers = new List<Session> { session };

        foreach (Session other in _parties.OthersOnline(session))
        {
            if (other.Record!.Zone == session.Record.Zone && other.Body != null && other.Body.GlobalPosition.DistanceTo(at) <= PartyFollowDistance)
            {
                travellers.Add(other);
            }
        }

        Travel(travellers, target, arrival, "You went with your party.");
    }

    // Moves players to another zone: doors, and taxi rides in and out. The first traveller
    // lands on the arrival; the rest around it, so a party does not land in one heap, and
    // they are told why they came along.
    private void Travel(List<Session> travellers, Zone target, Node3D arrival, string followerNotice)
    {
        using (Activity? span = ServerDiagnostics.Source.StartActivity("Travel"))
        {
            span?.SetTag("zone.target", target.ZoneId);
            span?.SetTag("travellers", travellers.Count);

            HashSet<string> left = new HashSet<string>();

            for (int i = 0; i < travellers.Count; i++)
            {
                Vector3 offset = Vector3.Zero;

                if (i > 0)
                {
                    float angle = Mathf.Tau * (i - 1) / (travellers.Count - 1);
                    offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 1.2f;

                    if (followerNotice.Length > 0)
                    {
                        _network.SendNotice(travellers[i].PeerId, followerNotice);
                    }
                }

                left.Add(travellers[i].Record!.Zone);
                Transfer(travellers[i], target, arrival, offset);
            }

            // Nothing in the old zones is shown to the travellers any more; the despawns go
            // out before the messages to change zone, so they arrive while the old zone is
            // still loaded.
            foreach (string zoneId in left)
            {
                _gate.Refresh(zoneId);
            }

            foreach (Session traveller in travellers)
            {
                _network.SendZoneChanged(traveller.PeerId, target.ZoneId);
            }
        }
    }

    // The body leaves its zone at once; the record says where the player arrives. The
    // client says WorldReady once it has loaded the zone, and the body is spawned there by
    // the same path as at login. A player still loading their last zone has no body yet.
    private void Transfer(Session session, Zone target, Node3D arrival, Vector3 offset)
    {
        GD.Print(session.Record!.DisplayName + " went from " + session.Record.Zone + " to " + target.ZoneId);
        _terminals.Disconnected(session);
        session.Body?.QueueFree();
        session.Body = null;

        session.Record.Zone = target.ZoneId;
        session.Record.PositionX = arrival.Position.X + offset.X;
        session.Record.PositionY = arrival.Position.Y;
        session.Record.PositionZ = arrival.Position.Z + offset.Z;
        session.Record.Yaw = arrival.Rotation.Y;
        session.State = SessionState.Accepted;
    }

    private Session? FindSession(long peer)
    {
        _sessions.TryGetValue(peer, out Session? session);
        return session;
    }

    // Runs a request for a peer that has a session; a request from anything else is
    // dropped, which is the closed-by-default answer.
    private void WithSession(long peer, Action<Session> handle)
    {
        Session? session = FindSession(peer);

        if (session != null && session.State == SessionState.InWorld)
        {
            handle(session);
        }
    }

    private void OnChatRequested(long peer, string text)
    {
        if (_sessions.TryGetValue(peer, out Session? session))
        {
            _chat.Say(session, text);
        }
    }

    private void OnItemPickedUp(Player player, GroundItem item)
    {
        if (!_sessions.TryGetValue(player.OwnerPeerId, out Session? session) || session.Inventory == null)
        {
            return;
        }

        session.Inventory.Add(item.Type, item.Tier, item.Quantity);
        player.Show(Gestures.PickUp);
        SendInventory(session);
        _network.SendNotice(session.PeerId, "Picked up " + item.Quantity + " " + ItemCatalog.Describe(item.Type, item.Tier));
    }

    // One line for load tests: who is here, how long a frame takes, and the traffic.
    // ENet counts bytes since the last time it was asked, so each line is its own span.
    private void PrintStats(double seconds)
    {
        int inWorld = 0;

        foreach (Session session in _sessions.Values)
        {
            if (session.State == SessionState.InWorld)
            {
                inWorld++;
            }
        }

        double sent = 0;
        double received = 0;

        if (_peer != null)
        {
            sent = _peer.Host.PopStatistic(ENetConnection.HostStatistic.SentData);
            received = _peer.Host.PopStatistic(ENetConnection.HostStatistic.ReceivedData);
        }

        ulong physicsFrames = Engine.GetPhysicsFrames();
        double steps = (physicsFrames - _physicsFramesAtStats) / seconds;
        _physicsFramesAtStats = physicsFrames;
        double process = Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000.0;
        double physics = Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000.0;

        GD.Print(string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "Stats: {0} connected, {1} in world, {2} fps, {7:F0} physics steps/s, frame {3:F1} ms, physics {4:F1} ms, out {5:F0} KB/s, in {6:F0} KB/s",
            _sessions.Count,
            inWorld,
            Engine.GetFramesPerSecond(),
            process,
            physics,
            sent / seconds / 1024.0,
            received / seconds / 1024.0,
            steps));
    }

    private void SendClock(Session session)
    {
        _network.SendClock(session.PeerId, _clock.SecondsOfDay(DateTime.UtcNow));
    }

    private void SendInventory(Session session)
    {
        string[] ids;
        int[] meta;
        float[] charges;
        InstanceWire.Pack(session.Instances, out ids, out meta, out charges);
        _network.SendInventory(session.PeerId, InventoryWire.Pack(session.Inventory!.Stacks), session.Dollars, ids, meta, charges);
    }

    private bool CanSee(long viewer, string zoneId)
    {
        return _sessions.TryGetValue(viewer, out Session? session)
            && session.State == SessionState.InWorld
            && session.ZoneId == zoneId;
    }

    private void Refuse(long peer, string reason)
    {
        _network.SendLoginRefused(peer, reason);
        GD.Print("Peer " + peer + " refused: " + reason);

        if (_sessions.TryGetValue(peer, out Session? session))
        {
            session.State = SessionState.Connected;
            session.AccountId = null;
        }
    }

    private void SaveEveryone()
    {
        using (Activity? span = ServerDiagnostics.Source.StartActivity("SaveEveryone"))
        {
            foreach (Session session in _sessions.Values)
            {
                if (session.HasEnteredWorld)
                {
                    Save(session);
                }
            }
        }
    }

    // Copies the player's state into a fresh record for the worker, so the worker never
    // reads an object the game thread is still changing. Between zones there is no body,
    // and the record already holds where they will arrive.
    private void Save(Session session)
    {
        PlayerRecord live = session.Record!;
        Player? body = session.Body;

        PlayerRecord snapshot = new PlayerRecord
        {
            PlayerId = live.PlayerId,
            AccountId = live.AccountId,
            DisplayName = live.DisplayName,
            Zone = live.Zone,
            PositionX = body != null ? body.Position.X : live.PositionX,
            PositionY = body != null ? body.Position.Y : live.PositionY,
            PositionZ = body != null ? body.Position.Z : live.PositionZ,
            Yaw = body != null ? body.Rotation.Y : live.Yaw,
            Dollars = session.Dollars,
        };

        foreach (ItemStack stack in session.Inventory!.Stacks)
        {
            snapshot.Stacks.Add(new ItemStack(stack.Type, stack.Tier, stack.Quantity));
        }

        foreach (ItemInstance instance in session.Instances)
        {
            snapshot.Instances.Add(new ItemInstance(instance.Id, instance.Type, instance.Tier)
            {
                ParentId = instance.ParentId,
                Slot = instance.Slot,
                Charge = instance.Charge,
            });
        }

        _worker.Enqueue(() => _players.Save(snapshot), e => GD.PrintErr("Saving " + snapshot.DisplayName + " failed: " + e.Message));
        _maps.Save(session);

        Guid playerId = live.PlayerId;
        PlayerProgress progress = session.Progress.Copy();
        _worker.Enqueue(() => _progressStore.Save(playerId, progress), e => GD.PrintErr("Saving the progress of " + snapshot.DisplayName + " failed: " + e.Message));
    }
}
