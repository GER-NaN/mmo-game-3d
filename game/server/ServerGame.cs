namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Data;
using MmoGame3d.Data.Accounts;
using MmoGame3d.Data.Players;
using MmoGame3d.Interact;
using MmoGame3d.Items;
using MmoGame3d.Networking;
using MmoGame3d.Players;
using MmoGame3d.Rules;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Players;
using MmoGame3d.Rules.Time;
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

    private static readonly TimeSpan ShutdownDrain = TimeSpan.FromSeconds(10);
    private static readonly PackedScene PlayerScene = GD.Load<PackedScene>("res://game/player/Player.tscn");

    private readonly Dictionary<long, Session> _sessions = new Dictionary<long, Session>();

    private LaunchOptions _options = null!;
    private Network _network = null!;
    private World _world = null!;
    private PersistenceWorker _worker = null!;
    private AccountStore _accounts = null!;
    private PlayerStore _players = null!;
    private StopSignals? _stopSignals;
    private VisibilityGate _gate = null!;
    private GroundItems _groundItems = null!;
    private ServerChat _chat = null!;
    private ServerParties _parties = null!;
    private ServerTerminals _terminals = null!;
    private ServerInteractions _interactions = null!;
    private WorldClock _clock = null!;
    private bool _stocked;
    private double _sinceSave;
    private double _sinceClock;

    public void Start(LaunchOptions options, Networks networks, World world)
    {
        Network network = networks.Session;
        PartyNetwork partyNetwork = networks.Party;
        _options = options;
        _network = network;
        _world = world;

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
        _worker = new PersistenceWorker();
        _stopSignals = new StopSignals(options.Port);
        _clock = new WorldClock(options.TimeZone, options.TimeOffsetHours);
        TimeSpan worldTime = TimeSpan.FromSeconds(_clock.SecondsOfDay(DateTime.UtcNow));
        GD.Print("World time " + worldTime.ToString(@"hh\:mm") + " (" + options.TimeZone + (options.TimeOffsetHours != 0 ? ", shifted " + options.TimeOffsetHours + " h" : "") + ")");
        _gate = new VisibilityGate(CanSee);
        _groundItems = new GroundItems(_gate, OnItemPickedUp);
        _chat = new ServerChat(network, () => _sessions.Values);
        _parties = new ServerParties(partyNetwork, network, _chat, () => _sessions.Values);
        _terminals = new ServerTerminals(networks.Terminal, network, () => _sessions.Values);
        _interactions = new ServerInteractions(world, network, _terminals);

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

        Multiplayer.MultiplayerPeer = peer;
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
        _network.LoginRequested += OnLoginRequested;
        _network.WorldReadyReceived += OnWorldReady;
        _network.ChatRequested += OnChatRequested;
        partyNetwork.InviteRequested += (peer, target) => WithSession(peer, session => _parties.Invite(session, FindSession(target)));
        partyNetwork.ResponseReceived += (peer, inviter, accept) => WithSession(peer, session => _parties.Respond(session, inviter, accept));
        partyNetwork.LeaveRequested += peer => WithSession(peer, session => _parties.Leave(session));
        partyNetwork.ChatRequested += (peer, text) => WithSession(peer, session => _parties.Chat(session, text));
        _network.InteractRequested += (peer, name) => WithSession(peer, session => _interactions.Use(session, name));
        networks.Terminal.LeaveRequested += peer => WithSession(peer, session => _terminals.Leave(session));

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

        if (session.HasEnteredWorld)
        {
            _terminals.Disconnected(session);
            Save(session);
            session.Body?.QueueFree();
            _parties.LeftWorld(session);
            _chat.Announce(session.Record!.DisplayName + " left.");
        }

        GD.Print("Peer " + peer + " left" + (session.Record != null ? " (" + session.Record.DisplayName + ")" : ""));
    }

    private void OnLoginRequested(long peer, int protocol, string licenseKeyText, string displayName)
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

        string? nameProblem = DisplayName.Problem(displayName);

        if (nameProblem != null)
        {
            Refuse(peer, nameProblem);
            return;
        }

        session.State = SessionState.LoggingIn;

        Zone start = _world.GetZone(ZoneIds.Start)!;
        Vector3 spawn = start.SpawnPoint;
        string name = displayName.Trim();

        _worker.Enqueue(
            () =>
            {
                Guid accountId = _accounts.GetOrCreate(licenseKey);
                PlayerRecord newPlayer = new PlayerRecord
                {
                    PlayerId = Guid.NewGuid(),
                    AccountId = accountId,
                    DisplayName = name,
                    Zone = ZoneIds.Start,
                    PositionX = spawn.X,
                    PositionY = spawn.Y,
                    PositionZ = spawn.Z,
                };
                return _players.GetOrCreate(newPlayer, out _);
            },
            record => OnPlayerLoaded(peer, record),
            e =>
            {
                GD.PrintErr("Login for peer " + peer + " failed: " + e.Message);
                Refuse(peer, "The server could not load your player. Try again.");
            });
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
        session.Inventory = new Inventory();

        foreach (ItemStack stack in record.Stacks)
        {
            session.Inventory.Add(stack.Type, stack.Tier, stack.Quantity);
        }

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
        body.Position = SpaceQueries.FreeSpotNear(zone, new Vector3(record.PositionX, record.PositionY, record.PositionZ));
        body.Rotation = new Vector3(0f, record.Yaw, 0f);
        body.RespawnPoint = zone.SpawnPoint;

        _gate.Watch(body.Synchronizer, zone.ZoneId);
        zone.Players.AddChild(body, true);
        session.Body = body;
        session.State = SessionState.InWorld;

        // The newcomer can now see the zone: everything there asks its filter again, so
        // the players and items already standing there spawn on this client.
        _gate.Refresh(zone.ZoneId);
        SendInventory(session);
        SendClock(session);
        _parties.EnteredWorld(session);

        if (!session.HasEnteredWorld)
        {
            session.HasEnteredWorld = true;
            _chat.Announce(record.DisplayName + " joined.");
        }
    }

    // Through a door: the body leaves this zone at once, and the client is told to load
    // the next. The client says WorldReady when it has, and the body is spawned there by
    // the same path as at login.
    private void OnDoorEntered(Door door, Player player)
    {
        Session? session = FindSession(player.OwnerPeerId);
        Zone? target = _world.GetZone(door.TargetZone);
        Node3D? arrival = target?.Arrival(door.TargetArrival);

        if (session == null || session.State != SessionState.InWorld || session.Record == null || target == null || arrival == null)
        {
            if (target == null || arrival == null)
            {
                GD.PrintErr("Door " + door.GetPath() + " leads to " + door.TargetZone + "/" + door.TargetArrival + ", which is not there");
            }

            return;
        }

        string from = session.Record.Zone;
        _terminals.Disconnected(session);
        session.Body!.QueueFree();
        session.Body = null;

        session.Record.Zone = target.ZoneId;
        session.Record.PositionX = arrival.Position.X;
        session.Record.PositionY = arrival.Position.Y;
        session.Record.PositionZ = arrival.Position.Z;
        session.Record.Yaw = arrival.Rotation.Y;
        session.State = SessionState.Accepted;

        // Nothing in the old zone is shown to the traveller any more; the despawns go out
        // before the message to change zone, so they arrive while the old zone is loaded.
        _gate.Refresh(from);
        _network.SendZoneChanged(session.PeerId, target.ZoneId);
        GD.Print(session.Record.DisplayName + " went from " + from + " to " + target.ZoneId);
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
        SendInventory(session);
        _network.SendNotice(session.PeerId, "Picked up " + item.Quantity + " " + ItemCatalog.Describe(item.Type, item.Tier));
    }

    private void SendClock(Session session)
    {
        _network.SendClock(session.PeerId, _clock.SecondsOfDay(DateTime.UtcNow));
    }

    private void SendInventory(Session session)
    {
        _network.SendInventory(session.PeerId, InventoryWire.Pack(session.Inventory!.Stacks));
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
        }
    }

    private void SaveEveryone()
    {
        foreach (Session session in _sessions.Values)
        {
            if (session.HasEnteredWorld)
            {
                Save(session);
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
        };

        foreach (ItemStack stack in session.Inventory!.Stacks)
        {
            snapshot.Stacks.Add(new ItemStack(stack.Type, stack.Tier, stack.Quantity));
        }

        _worker.Enqueue(() => _players.Save(snapshot), e => GD.PrintErr("Saving " + snapshot.DisplayName + " failed: " + e.Message));
    }
}
