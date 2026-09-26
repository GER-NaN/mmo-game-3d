namespace MmoGame3d.Client;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Dev;
using MmoGame3d.Gardening;
using MmoGame3d.Networking;
using MmoGame3d.Rules;
using MmoGame3d.Rules.Chat;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Players;
using MmoGame3d.Rules.Shops;
using MmoGame3d.Rules.Social;
using MmoGame3d.Rules.Terminals;
using MmoGame3d.Rules.Town;
using MmoGame3d.Rules.World;
using MmoGame3d.Ui;
using MmoGame3d.Zones;

/// <summary>
/// The client's side of the game: the menus, connecting and logging in, and loading
/// the world once the server accepts. The world goes under Main as "World", the same
/// path as on the server, so synced nodes find their place.
/// </summary>
public partial class ClientGame : Node
{
    private static readonly PackedScene MainMenuScene = GD.Load<PackedScene>("res://game/ui/MainMenu.tscn");
    private static readonly PackedScene SettingsScene = GD.Load<PackedScene>("res://game/ui/SettingsPanel.tscn");
    private static readonly PackedScene InGameMenuScene = GD.Load<PackedScene>("res://game/ui/InGameMenu.tscn");
    private static readonly PackedScene HudScene = GD.Load<PackedScene>("res://game/ui/Hud.tscn");
    private static readonly PackedScene InventoryScene = GD.Load<PackedScene>("res://game/ui/InventoryPanel.tscn");
    private static readonly PackedScene ChatScene = GD.Load<PackedScene>("res://game/ui/ChatBox.tscn");
    private static readonly PackedScene TerminalScene = GD.Load<PackedScene>("res://game/ui/TerminalScreen.tscn");
    private static readonly PackedScene ShopScene = GD.Load<PackedScene>("res://game/ui/ShopPanel.tscn");
    private static readonly PackedScene WorkbenchScene = GD.Load<PackedScene>("res://game/ui/WorkbenchPanel.tscn");
    private static readonly PackedScene GiveScene = GD.Load<PackedScene>("res://game/ui/GivePanel.tscn");
    private static readonly PackedScene SelectScene = GD.Load<PackedScene>("res://game/ui/CharacterSelect.tscn");
    private static readonly PackedScene CreatorScene = GD.Load<PackedScene>("res://game/ui/CharacterCreator.tscn");
    private static readonly PackedScene RecyclerScene = GD.Load<PackedScene>("res://game/ui/RecyclerPanel.tscn");
    private static readonly PackedScene GardenScene = GD.Load<PackedScene>("res://game/gardening/GardenScreen.tscn");
    private static readonly PackedScene MapScene = GD.Load<PackedScene>("res://game/ui/MapPanel.tscn");
    private static readonly PackedScene SocialScene = GD.Load<PackedScene>("res://game/ui/SocialPanel.tscn");
    private static readonly PackedScene SkillsScene = GD.Load<PackedScene>("res://game/ui/SkillsPanel.tscn");
    private static readonly PackedScene CollegeScene = GD.Load<PackedScene>("res://game/ui/CollegePanel.tscn");

    // Walking this far from where a shop or a workbench was opened closes it.
    private const float PanelWalkAway = 3f;

    // How much chat the client keeps, for a screen opened later.
    private const int ChatKept = 100;
    private static readonly PackedScene WorldScene = GD.Load<PackedScene>("res://game/zones/World.tscn");

    private LaunchOptions _options = null!;
    private Network _network = null!;
    private PartyNetwork _partyNetwork = null!;
    private TerminalNetwork _terminalNetwork = null!;
    private ShopNetwork _shopNetwork = null!;
    private ItemNetwork _itemNetwork = null!;
    private SocialNetwork _socialNetwork = null!;
    private ProgressNetwork _progressNetwork = null!;
    private GardenNetwork _gardenNetwork = null!;
    private Networks _networks = null!;
    private SubwayNetwork _subwayNetwork = null!;
    private VisitorBookPanel? _book;
    private GardenScreen? _garden;
    private PlantCard? _plantCard;
    private Node _main = null!;
    private ClientSettings _settings = null!;
    private Profile _profile = null!;
    private CanvasLayer _ui = null!;

    private MainMenu? _menu;
    private InGameMenu? _inGameMenu;
    private World? _world;
    private Hud? _hud;
    private InventoryPanel? _inventoryPanel;
    private ChatBox? _chat;
    private ClientParty? _party;
    private InteractionFinder? _finder;
    private TerminalScreen? _terminal;
    private ShopPanel? _shop;
    private string _shopId = "";
    private WorkbenchPanel? _workbench;
    private GivePanel? _give;
    private MapPanel? _map;
    private RecyclerPanel? _recycler;
    private SocialPanel? _social;
    private SkillsPanel? _skills;
    private CollegePanel? _college;

    // What the server last said about this player's progress; see ProgressNetwork.
    private int[] _skillIds = new int[0];
    private long[] _skillXp = new long[0];
    private int _career = -1;
    private long _careerXp;
    private int _careerRank;
    private bool _classTaken;
    private int _level = 1;
    private string[] _achievements = new string[0];

    // What the server last said about friends and ignores; see SocialNetwork.
    private string[][] _contacts = { new string[0], new string[0], new string[0], new string[0], new string[0] };

    // Where the server says this player has been, per zone.
    private readonly Dictionary<string, byte[]> _maps = new Dictionary<string, byte[]>();
    private Players.Player? _giveTo;
    private Vector3 _panelOpenedAt;
    private List<ItemInstance> _instances = new List<ItemInstance>();
    private ClientIntents? _intents;
    private int _dollars;
    private readonly List<ChatLine> _chatLog = new List<ChatLine>();
    private BotDriver? _bot;

    // What the server last said this player carries.
    private List<ItemStack> _stacks = new List<ItemStack>();
    private CharacterSelect? _select;
    private CharacterCreator? _creator;

    // Bots and --autoconnect choose by themselves: make a character if there is none,
    // then play the first. Once each.
    private bool _autoCreated;
    private bool _autoPlayed;
    private string _zoneId = "";

    // The street lights job, taken in a terminal: shown on the HUD until done.
    private bool _hasLightsJob;
    private bool _hasTaxiJob;
    private string _displayName = "";
    private string _address = "";

    public void Start(LaunchOptions options, Networks networks, Node main)
    {
        _options = options;
        _network = networks.Session;
        _partyNetwork = networks.Party;
        _terminalNetwork = networks.Terminal;
        _shopNetwork = networks.Shop;
        _itemNetwork = networks.Items;
        _socialNetwork = networks.Social;
        _progressNetwork = networks.Progress;
        _gardenNetwork = networks.Garden;
        _networks = networks;
        _subwayNetwork = networks.Subway;
        _subwayNetwork.PageReceived += ShowBook;
        _main = main;
        _profile = new Profile(options.Profile);
        _settings = ClientSettings.Load();

        if (DisplayServer.GetName() != "headless")
        {
            _settings.Apply();
        }
        else
        {
            // A headless client's window is 64 by 64 pixels, so centred panels hang off
            // its edges and a bot's click lands beside the button. Give it a real size.
            GetTree().Root.Size = new Vector2I(1280, 720);
        }

        _ui = new CanvasLayer { Name = "Ui" };
        AddChild(_ui);

        if (options.ReportEverySeconds > 0)
        {
            WorldReport report = new WorldReport { Name = "WorldReport" };
            AddChild(report);
            report.Start(options.ReportEverySeconds);
        }

        _network.LoginAccepted += (zoneId, displayName) => Callable.From(() => OnLoginAccepted(zoneId, displayName)).CallDeferred();
        _network.LoginRefused += reason => Callable.From(() => OnLoginRefused(reason)).CallDeferred();
        _network.InventoryReceived += OnInventoryReceived;
        _network.NoticeReceived += OnNoticeReceived;
        _network.ChatReceived += OnChatReceived;
        _network.DirectReceived += OnDirectReceived;
        _network.EmpPulseReceived += ShowEmpPulse;
        _network.DroneZapReceived += ShowDroneZap;
        _network.ClockReceived += OnClockReceived;
        _network.MapReceived += OnMapReceived;
        _socialNetwork.ContactsReceived += OnContactsReceived;
        _socialNetwork.WhoisResultsReceived += (ids, names, titles, levels, online) => _terminal?.ShowWhoisResults(ids, names, titles, levels, online);
        _socialNetwork.WhoisPageReceived += page =>
        {
            GD.Print("Whois page: " + page["name"]);
            _terminal?.ShowWhoisPage(page);
        };
        _progressNetwork.ProgressReceived += OnProgressReceived;
        _progressNetwork.CollegeOpened += OnCollegeOpened;
        _progressNetwork.AchievementsReceived += earned =>
        {
            _achievements = earned;
            _skills?.ShowAchievements(earned);
        };
        _gardenNetwork.GardenOpened += OpenGarden;
        _gardenNetwork.PlantMade += (id, name, reward) =>
        {
            GD.Print("Made house plant #" + id + (name.Length > 0 ? " '" + name + "'" : "") + "; reward " + reward);
            _garden?.ShowMade(id, name, reward);
        };
        _gardenNetwork.CardReceived += ShowPlantCard;
        _network.ZoneChanged += zoneId => Callable.From(() => OnZoneChanged(zoneId)).CallDeferred();
        _shopNetwork.ShopOpened += OnShopOpened;
        _itemNetwork.WorkbenchOpened += OnWorkbenchOpened;
        _itemNetwork.RecyclerOpened += OnRecyclerOpened;
        _network.IntentAnswered += (id, refusal) => _intents?.Answer(id, refusal);
        _terminalNetwork.Opened += OnTerminalOpened;
        _terminalNetwork.Closed += CloseTerminal;
        _terminalNetwork.RosterReceived += (names, zones, online) => _terminal?.ShowRoster(names, zones, online);
        _terminalNetwork.TownReceived += (working, taken, taxisClean, taxiTaken, log) =>
        {
            _terminal?.ShowTown(working, taken, taxisClean, taxiTaken, log);
            _hasTaxiJob = taxiTaken && !taxisClean;

            if (taken && !working && !_hasLightsJob)
            {
                GD.Print("Job on the HUD: the junction box");
            }

            _hasLightsJob = taken && !working;
        };
        _terminalNetwork.BoardReceived += (objective, lines) =>
        {
            GD.Print("Leaderboard " + objective + ": " + string.Join(" | ", lines));
            _terminal?.ShowBoard(objective, lines);
        };
        _terminalNetwork.DefenseSeedReceived += (seed, lengthMs) =>
        {
            GD.Print("Agent Defense: run with seed " + seed + ", " + lengthMs + " ms");
            _terminal?.PlayDefense(seed, lengthMs);
        };
        _terminalNetwork.StatusReceived += lines =>
        {
            GD.Print("Status board: " + (lines.Length > 0 ? lines[0] : "all quiet"));
            _terminal?.ShowStatus(lines);
        };
        _terminalNetwork.CrackReceived += (guesses, exact, partial, positions, left, status) => _terminal?.ShowCrack(guesses, exact, partial, positions, left, status);
        // Deferred, like the login answers above: these fire inside the engine's network
        // poll, and closing the peer or freeing the world is better done after it.
        Multiplayer.ConnectedToServer += () => Callable.From(OnConnected).CallDeferred();
        Multiplayer.ConnectionFailed += () => Callable.From(OnConnectionFailed).CallDeferred();
        Multiplayer.ServerDisconnected += () => Callable.From(OnServerDisconnected).CallDeferred();

        _network.CharactersReceived += (ids, names, looks, levels, titles, message) => Callable.From(() => OnCharactersReceived(ids, names, looks, levels, titles, message)).CallDeferred();

        if (options.Garden)
        {
            _garden = GardenScene.Instantiate<GardenScreen>();
            _ui.AddChild(_garden);

            if (options.ScreenshotPath != null)
            {
                Screenshot shot = new Screenshot { Name = "Screenshot" };
                AddChild(shot);
                shot.Start(options.ScreenshotPath, false, options.ScreenshotAfterSeconds);
            }
        }
        else if (options.Creator)
        {
            CharacterCreator creator = CreatorScene.Instantiate<CharacterCreator>();
            _ui.AddChild(creator);
            creator.Open(true, DefaultName(), options.Look);

            if (options.ScreenshotPath != null)
            {
                Screenshot shot = new Screenshot { Name = "Screenshot" };
                AddChild(shot);
                shot.Start(options.ScreenshotPath, false, options.ScreenshotAfterSeconds);
            }
        }
        else if (options.AutoConnect)
        {
            Connect(options.Address ?? _settings.Address);
        }
        else
        {
            ShowMainMenu("");
        }
    }

    public override void _Process(double delta)
    {
        if (_world != null && _hud != null)
        {
            _hud.ShowClock(_world.GetNode<DayNight>("DayNight").ClockText);
        }

        Node3D? self = GetTree().GetFirstNodeInGroup(Players.Player.LocalGroup) as Node3D;

        Players.Player? me = self as Players.Player;

        if (me != null)
        {
            _hud?.ShowHealth(me.Health);
        }

        ShowJob(self);

        if ((_shop != null || _workbench != null || _give != null || _college != null || _recycler != null || _plantCard != null || _book != null) && self != null && self.GlobalPosition.DistanceTo(_panelOpenedAt) > PanelWalkAway)
        {
            CloseBook();
            CloseShop();
            CloseWorkbench();
            CloseGive();
            CloseCollege();
            CloseRecycler();
            ClosePlantCard();
        }

        // The one being given to walked off, left, or changed zone.
        if (_give != null && (_giveTo == null || !IsInstanceValid(_giveTo)))
        {
            CloseGive();
        }
    }

    // The job line, top right: what to do, and in town which way and how far.
    private void ShowJob(Node3D? self)
    {
        Town.TownState? town = GetTree().GetFirstNodeInGroup(Town.TownState.Group) as Town.TownState;

        if (town != null && town.LightsWorking)
        {
            _hasLightsJob = false;
        }

        if (town != null && town.TaxisClean)
        {
            _hasTaxiJob = false;
        }

        Town.JunctionBox.Marked = _hasLightsJob;

        if (_hud == null)
        {
            return;
        }

        string taxiJob = _hasTaxiJob ? "Job: crack a code at a public terminal to clean the robo taxis' rootkit" : "";

        if (!_hasLightsJob)
        {
            _hud.ShowJob(taxiJob);
            return;
        }

        string text = "Job: repair the junction box on Main Street, west of the crossing (a RAM stick as the part)";
        Node3D? box = _world?.GetZone(_zoneId)?.GetNodeOrNull<Node3D>("Interactables/JunctionBox");
        Camera3D? camera = GetViewport().GetCamera3D();

        if (box != null && self != null && camera != null)
        {
            Vector3 to = box.GlobalPosition - self.GlobalPosition;
            to.Y = 0f;
            Vector3 ahead = -camera.GlobalBasis.Z;
            ahead.Y = 0f;
            text = "Job: repair the junction box, " + Mathf.RoundToInt(to.Length()) + " m " + Direction(ahead, to);
        }

        _hud.ShowJob(taxiJob.Length > 0 ? text + "\n" + taxiJob : text);
    }

    // Which way a spot is from where the camera looks, in words.
    private static string Direction(Vector3 ahead, Vector3 to)
    {
        if (to.Length() < 2f || ahead.Length() < 0.01f)
        {
            return "here";
        }

        // Positive is to the left, seen from above.
        float angle = Mathf.RadToDeg(ahead.SignedAngleTo(to, Vector3.Up));

        if (Mathf.Abs(angle) < 25f)
        {
            return "ahead";
        }

        if (Mathf.Abs(angle) > 155f)
        {
            return "behind you";
        }

        string side = angle > 0 ? "left" : "right";

        if (Mathf.Abs(angle) < 65f)
        {
            return "ahead, to the " + side;
        }

        return Mathf.Abs(angle) < 115f ? "to the " + side : "behind, to the " + side;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // At the potting table the keys belong to it; it handles its own Esc.
        if (_world == null || _garden != null)
        {
            return;
        }

        if (_terminal != null)
        {
            // Esc goes offline; the other keys belong to the terminal.
            if (@event.IsActionPressed("ui_cancel"))
            {
                GetViewport().SetInputAsHandled();
                _terminalNetwork.SendLeave();
            }

            return;
        }

        if ((_shop != null || _workbench != null || _give != null || _map != null || _college != null || _recycler != null || _plantCard != null || _book != null) && @event.IsActionPressed("ui_cancel"))
        {
            CloseBook();
            GetViewport().SetInputAsHandled();
            CloseShop();
            CloseWorkbench();
            CloseGive();
            CloseMap();
            CloseCollege();
            CloseRecycler();
            ClosePlantCard();
            return;
        }

        if (@event.IsActionPressed("phone"))
        {
            GetViewport().SetInputAsHandled();
            _itemNetwork.SendUsePhone();
            return;
        }

        if (@event.IsActionPressed("ui_cancel"))
        {
            GetViewport().SetInputAsHandled();

            if (_inGameMenu == null)
            {
                OpenInGameMenu();
            }
            else
            {
                CloseInGameMenu();
            }
        }
        else if (@event.IsActionPressed("inventory"))
        {
            GetViewport().SetInputAsHandled();
            ToggleInventory();
        }
        else if (@event.IsActionPressed("map"))
        {
            GetViewport().SetInputAsHandled();
            ToggleMap();
        }
        else if (@event.IsActionPressed("social"))
        {
            GetViewport().SetInputAsHandled();
            ToggleSocial();
        }
        else if (@event.IsActionPressed("skills"))
        {
            GetViewport().SetInputAsHandled();
            ToggleSkills();
        }
        else if (@event.IsActionPressed("emp"))
        {
            GetViewport().SetInputAsHandled();
            _network.SendFireEmp();
        }
        else if (@event.IsActionPressed("chat") && _chat != null)
        {
            GetViewport().SetInputAsHandled();
            _chat.Open();
        }
    }

    private string DefaultName()
    {
        if (_options.DisplayName != null)
        {
            return _options.DisplayName;
        }

        string saved = _settings.NameFor(_profile.Name);
        return saved.Length > 0 ? saved : _profile.Name;
    }

    private void ShowMainMenu(string status)
    {
        if (_menu == null)
        {
            _menu = MainMenuScene.Instantiate<MainMenu>();
            _ui.AddChild(_menu);
            _menu.PlayPressed += Connect;
            _menu.SettingsPressed += OpenSettings;
            _menu.QuitPressed += Quit;
        }

        CloseCharacterScreens();
        _menu.Fill(_profile.Name, _settings.Address);
        _menu.SetStatus(status);
        _menu.SetBusy(false);
    }

    private void Connect(string address)
    {
        _autoCreated = false;
        _autoPlayed = false;
        _address = address;
        _settings.Address = address;
        _settings.Save();

        ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
        Error error = peer.CreateClient(address, _options.Port);

        if (error != Error.Ok)
        {
            ShowMainMenu("Could not start a connection to " + address + ": " + error);
            return;
        }

        Multiplayer.MultiplayerPeer = peer;
        _menu?.SetStatus("Connecting to " + address + "...");
        _menu?.SetBusy(true);
        GD.Print("Connecting to " + address + ":" + _options.Port + " as profile " + _profile.Name);
    }

    private void OnConnected()
    {
        GD.Print("Connected as peer " + Multiplayer.GetUniqueId() + "; saying hello");

        // Before the hello, so it is set when the player is loaded.
        if (_options.Scenario != null)
        {
            _network.SendScenario(_options.Scenario);
        }

        _network.SendHello(GameVersion.Protocol, _profile.LicenseKey().ToString());
    }

    private void OnConnectionFailed()
    {
        Disconnect();
        ShowMainMenu("Could not reach the server at " + _address + ".");
    }

    private void OnServerDisconnected()
    {
        LeaveWorld();
        Disconnect();
        ShowMainMenu("The connection to the server was lost.");
    }

    // The account's characters: the screen to choose one, or, for bots, a choice made.
    private void OnCharactersReceived(string[] ids, string[] names, string[] looks, int[] levels, string[] titles, string message)
    {
        GD.Print("Characters: " + string.Join(", ", names) + (message.Length > 0 ? " (" + message + ")" : ""));

        if (_options.AutoConnect && !_options.ShowCharacters)
        {
            if (ids.Length == 0 && !_autoCreated)
            {
                _autoCreated = true;
                _network.SendCreateCharacter(DefaultName(), _options.Look);
            }
            else if (ids.Length > 0 && !_autoPlayed)
            {
                _autoPlayed = true;
                _network.SendLogin(ids[0]);
            }

            return;
        }

        // A problem with a character being made is shown on the creator, which stays.
        if (_creator != null && message.Length > 0)
        {
            _creator.SetStatus(message);
            return;
        }

        if (_creator != null)
        {
            _creator.QueueFree();
            _creator = null;
        }

        if (_menu != null)
        {
            _menu.QueueFree();
            _menu = null;
        }

        if (_select == null)
        {
            _select = SelectScene.Instantiate<CharacterSelect>();
            _ui.AddChild(_select);
            _select.PlayPressed += id =>
            {
                GD.Print("Playing " + id);
                _network.SendLogin(id);
            };
            _select.CreatePressed += OpenCreator;
            _select.BackPressed += () =>
            {
                Disconnect();
                ShowMainMenu("");
            };
        }

        _select.Visible = true;
        _select.ShowCharacters(ids, names, looks, levels, titles, message);

        if (_options.ShowCharacters && _options.ScreenshotPath != null && GetNodeOrNull("Screenshot") == null)
        {
            Screenshot shot = new Screenshot { Name = "Screenshot" };
            AddChild(shot);
            shot.Start(_options.ScreenshotPath, false, _options.ScreenshotAfterSeconds);
        }
    }

    private void OpenCreator()
    {
        _creator = CreatorScene.Instantiate<CharacterCreator>();
        _ui.AddChild(_creator);
        _creator.Open(true, DefaultName(), Looks.Default);
        _creator.DonePressed += (name, look) =>
        {
            string? problem = DisplayName.Problem(name);

            if (problem != null)
            {
                _creator?.SetStatus(problem);
                return;
            }

            _creator?.SetStatus("Making " + name + "...");
            _network.SendCreateCharacter(name, look);
        };
        _creator.CancelPressed += () =>
        {
            _creator?.QueueFree();
            _creator = null;
        };
    }

    // In the world: change how your character looks.
    private void OpenWardrobe()
    {
        CloseInGameMenu();
        Players.Player? self = GetTree().GetFirstNodeInGroup(Players.Player.LocalGroup) as Players.Player;

        if (self == null || _creator != null)
        {
            return;
        }

        _creator = CreatorScene.Instantiate<CharacterCreator>();
        _ui.AddChild(_creator);
        _creator.Open(false, "", self.Look);
        _creator.DonePressed += (name, look) =>
        {
            _network.SendSetLook(look);
            _creator?.QueueFree();
            _creator = null;
        };
        _creator.CancelPressed += () =>
        {
            _creator?.QueueFree();
            _creator = null;
        };
    }

    private void CloseCharacterScreens()
    {
        if (_select != null)
        {
            _select.QueueFree();
            _select = null;
        }

        if (_creator != null)
        {
            _creator.QueueFree();
            _creator = null;
        }
    }

    private void OnLoginRefused(string reason)
    {
        GD.Print("Login refused: " + reason);
        Disconnect();
        ShowMainMenu(reason);
    }

    private void OnLoginAccepted(string zoneId, string displayName)
    {
        GD.Print("Logged in as " + displayName + " in " + zoneId);

        if (!_profile.IsFresh)
        {
            _settings.SetNameFor(_profile.Name, displayName);
            _settings.Save();
        }

        if (_menu != null)
        {
            _menu.QueueFree();
            _menu = null;
        }

        CloseCharacterScreens();
        _world = WorldScene.Instantiate<World>();
        _world.Name = "World";
        _main.AddChild(_world);
        _world.LoadZone(zoneId, Vector3.Zero);
        _zoneId = zoneId;
        _displayName = displayName;

        _hud = HudScene.Instantiate<Hud>();
        _ui.AddChild(_hud);
        _hud.ShowIdentity(displayName, ZoneIds.DisplayName(zoneId));
        _world.GetNode<Players.ChaseCamera>("Camera").Zoomed += distance => _settings.CameraDistance = distance;
        _hud.ActionPressed += OnHudAction;
        ApplyControls();

        _chat = ChatScene.Instantiate<ChatBox>();
        _ui.AddChild(_chat);
        _chat.Submitted += OnChatSubmitted;
        _chat.DirectSubmitted += _network.SendDirect;

        _party = new ClientParty { Name = "Party" };
        AddChild(_party);
        _party.Start(_partyNetwork, _ui, _world);
        _party.GiveRequested += OpenGive;
        _party.FriendRequested += player => _socialNetwork.SendBefriend(player.OwnerPeerId);
        _party.IgnoreRequested += player => _socialNetwork.SendIgnore(player.OwnerPeerId);
        _party.MessageRequested += player => _chat?.OpenDirect(player.PlayerIdText, player.DisplayName);

        _intents = new ClientIntents { Name = "Intents" };
        AddChild(_intents);
        _intents.Answered += (label, refusal) =>
        {
            if (refusal.Length > 0)
            {
                OnNoticeReceived(refusal);
                _garden?.ShowProblem(refusal);
            }
        };

        _finder = new InteractionFinder { Name = "InteractionFinder" };
        AddChild(_finder);
        _finder.PromptChanged += _hud.ShowPrompt;
        _finder.UseRequested += _network.SendInteract;

        _network.SendWorldReady();

        if (_options.ScreenshotPath != null)
        {
            Screenshot shot = new Screenshot { Name = "Screenshot" };
            AddChild(shot);
            shot.Start(_options.ScreenshotPath, _options.Overview, _options.ScreenshotAfterSeconds);
        }

        if (_options.WalkTest)
        {
            AddChild(new WalkTest { Name = "WalkTest", Watch = _options.WatchTest });
        }

        if (_options.LoadBot)
        {
            LoadBot legs = new LoadBot(Name.GetHashCode() ^ _options.Profile.GetHashCode()) { Name = "LoadBot", Say = _network.SendChat };
            AddChild(legs);
        }

        if (_options.Bot)
        {
            _bot = new BotDriver { Name = "Bot", Say = _network.SendChat };
            AddChild(_bot);
        }

        if (_options.Scenario != null && GetNodeOrNull("Scenario") == null)
        {
            AddChild(new ScenarioDriver(_options.Scenario, _networks) { Name = "Scenario" });
        }
    }

    private void OnInventoryReceived(int[] packed, int dollars, string[] ids, int[] meta, float[] charges)
    {
        _stacks = InventoryWire.Unpack(packed);
        _dollars = dollars;
        _instances = InstanceWire.Unpack(ids, meta, charges);
        _inventoryPanel?.ShowBag(_stacks, _dollars, _instances);
        _workbench?.ShowBench(_stacks, _instances);

        if (_give != null && _giveTo != null && IsInstanceValid(_giveTo))
        {
            _give.ShowFor(_giveTo.DisplayName, _stacks, _dollars);
        }
        _hud?.ShowDollars(dollars);
        _shop?.ShowDollars(dollars);
        _recycler?.ShowBag(_stacks, _instances, _dollars);
        ShowBattery();
    }

    private void OpenGarden()
    {
        CloseGarden();
        ClosePanels();
        _garden = GardenScene.Instantiate<GardenScreen>();
        _ui.AddChild(_garden);
        _garden.CompletePressed += (design, name) => _intents?.Start("plant", id => _gardenNetwork.SendComplete(id, design, name));
        _garden.Closed += CloseGarden;

        if (_finder != null)
        {
            _finder.Paused = true;
        }

        _hud?.ShowPrompt("");
    }

    private void CloseGarden()
    {
        if (_garden == null)
        {
            return;
        }

        _garden.QueueFree();
        _garden = null;

        if (_finder != null)
        {
            _finder.Paused = false;
        }
    }

    private void ShowPlantCard(long plantId, string name, string creator, string madeOn, string design, string[] history)
    {
        ClosePanels();
        GD.Print("Plant card: #" + plantId + ", created by " + creator + ", " + history.Length + " history lines");
        _panelOpenedAt = SelfPosition();
        _plantCard = new PlantCard();
        _ui.AddChild(_plantCard);
        _plantCard.ShowPlant(plantId, name, creator, madeOn, design, history);
        _plantCard.Closed += ClosePlantCard;
    }

    private void ClosePlantCard()
    {
        if (_plantCard != null)
        {
            _plantCard.QueueFree();
            _plantCard = null;
        }
    }

    private void OnRecyclerOpened()
    {
        ClosePanels();
        _panelOpenedAt = SelfPosition();
        _recycler = RecyclerScene.Instantiate<RecyclerPanel>();
        _ui.AddChild(_recycler);
        _recycler.ShowBag(_stacks, _instances, _dollars);
        _recycler.RecycleOnePressed += (type, tier) => _intents?.Start("recycle", id => _itemNetwork.SendRecycleOne(id, (int)type, (int)tier));
        _recycler.RecycleThingPressed += thing => _intents?.Start("recycle", id => _itemNetwork.SendRecycleThing(id, thing.ToString()));
        _recycler.Closed += CloseRecycler;
    }

    private void CloseRecycler()
    {
        if (_recycler != null)
        {
            _recycler.QueueFree();
            _recycler = null;
        }
    }

    private void ShowBattery()
    {
        Belongings mine = new Belongings(new Inventory(), _instances);
        ItemInstance? phone = mine.Equipped(SlotType.Device);
        ItemInstance? battery = mine.DeviceBattery();
        _hud?.ShowBattery(phone == null ? -1 : (battery == null ? 0 : Power.Percent(battery.Charge)), ClientSettings.KeyName);
    }

    // The action bar: the same as the keys.
    private void OnHudAction(string action)
    {
        switch (action)
        {
            case "inventory":
                ToggleInventory();
                break;
            case "skills":
                ToggleSkills();
                break;
            case "social":
                ToggleSocial();
                break;
            case "map":
                ToggleMap();
                break;
            case "phone":
                _itemNetwork.SendUsePhone();
                break;
            case "ui_cancel":
                if (_inGameMenu == null)
                {
                    OpenInGameMenu();
                }
                else
                {
                    CloseInGameMenu();
                }

                break;
        }
    }

    private void OnWorkbenchOpened()
    {
        ClosePanels();
        _panelOpenedAt = SelfPosition();
        _workbench = WorkbenchScene.Instantiate<WorkbenchPanel>();
        _ui.AddChild(_workbench);
        _workbench.ShowBench(_stacks, _instances);
        _workbench.Closed += CloseWorkbench;
        _workbench.RemovePressed += id => _itemNetwork.SendRemoveBattery(id.ToString());
        _workbench.InsertPressed += (id, battery) => _itemNetwork.SendInsertBattery(id.ToString(), battery);
    }

    private void OpenGive(Players.Player target)
    {
        ClosePanels();
        _giveTo = target;
        _panelOpenedAt = SelfPosition();
        _give = GiveScene.Instantiate<GivePanel>();
        _ui.AddChild(_give);
        _give.ShowFor(target.DisplayName, _stacks, _dollars);
        _give.Closed += CloseGive;
        long peer = target.OwnerPeerId;
        _give.GivePressed += (type, tier, quantity) =>
            _intents?.Start("give", id => _itemNetwork.SendGive(id, peer, (int)type, (int)tier, quantity, 0));
        _give.GiveDollarsPressed += amount =>
            _intents?.Start("give", id => _itemNetwork.SendGive(id, peer, 0, 0, 0, amount));
    }

    private void CloseGive()
    {
        if (_give != null)
        {
            _give.QueueFree();
            _give = null;
        }

        _giveTo = null;
    }

    private void CloseWorkbench()
    {
        if (_workbench != null)
        {
            _workbench.QueueFree();
            _workbench = null;
        }
    }

    private Vector3 SelfPosition()
    {
        Node3D? self = GetTree().GetFirstNodeInGroup(Players.Player.LocalGroup) as Node3D;
        return self != null ? self.GlobalPosition : Vector3.Zero;
    }

    private void OnShopOpened(string shopId)
    {
        ClosePanels();
        _panelOpenedAt = SelfPosition();
        _shopId = shopId;
        _shop = ShopScene.Instantiate<ShopPanel>();
        _ui.AddChild(_shop);
        _shop.ShowShop(shopId, "Electronics shop", _dollars);
        _shop.Closed += CloseShop;
        _shop.BuyPressed += OnBuyPressed;
    }

    private void OnBuyPressed(int offerIndex)
    {
        ShopOffer? offer = Shops.Offer(_shopId, offerIndex);
        string shopId = _shopId;

        if (offer != null && _intents != null)
        {
            _intents.Start(ItemCatalog.Describe(offer.Type, offer.Tier), id => _shopNetwork.SendBuy(id, shopId, offerIndex));
        }
    }

    private void CloseShop()
    {
        if (_shop != null)
        {
            _shop.QueueFree();
            _shop = null;
        }
    }

    // Through a door: fade out, swap the zone, say ready, fade in. The server has already
    // taken this player's body out of the old zone.
    private void OnZoneChanged(string zoneId)
    {
        if (_world == null)
        {
            return;
        }

        CloseTerminal();
        CloseMap();
        CloseCollege();
        CloseGarden();
        ClosePlantCard();
        ColorRect fade = new ColorRect { Color = new Color(0f, 0f, 0f, 0f), MouseFilter = Control.MouseFilterEnum.Ignore };
        fade.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _ui.AddChild(fade);

        Tween tween = fade.CreateTween();
        tween.TweenProperty(fade, "color:a", 1f, 0.25f);
        tween.TweenCallback(Callable.From(() =>
        {
            if (_world == null)
            {
                return;
            }

            _world.UnloadZone(_zoneId);
            _world.LoadZone(zoneId, Vector3.Zero);
            _zoneId = zoneId;
            _hud?.ShowIdentity(_displayName, ZoneIds.DisplayName(zoneId));
            _network.SendWorldReady();
            GD.Print("Now in " + zoneId);
        }));
        tween.TweenProperty(fade, "color:a", 0f, 0.4f);
        tween.TweenCallback(Callable.From(fade.QueueFree));
    }

    private void OnClockReceived(double secondsOfDay)
    {
        _world?.GetNode<DayNight>("DayNight").SetTime(secondsOfDay);
    }

    // An emote ("/wave") plays; "/p " speaks to the party; anything else to everyone.
    private void OnChatSubmitted(string text)
    {
        string? emote = Gestures.EmoteIn(text);

        if (emote != null)
        {
            _network.SendEmote(emote);
        }
        else if (text.StartsWith("/p ", System.StringComparison.OrdinalIgnoreCase) && _party != null)
        {
            _party.SendChat(text.Substring(3));
        }
        else
        {
            _network.SendChat(text);
        }
    }

    // A red beam from the drone to whoever it hit, gone in a moment. A placeholder look.
    private void ShowDroneZap(Vector3 from, Vector3 to)
    {
        if (_world == null)
        {
            return;
        }

        float length = from.DistanceTo(to);
        StandardMaterial3D material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            AlbedoColor = new Color(1f, 0.2f, 0.15f, 0.9f),
        };
        MeshInstance3D beam = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.03f, BottomRadius = 0.03f, Height = length, Material = material },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        _world.AddChild(beam);

        // A cylinder stands along Y: turn Y onto the line between the two ends.
        Vector3 along = (to - from).Normalized();
        Vector3 side = along.Cross(Vector3.Up).Length() > 0.01f ? along.Cross(Vector3.Up).Normalized() : Vector3.Right;
        beam.GlobalTransform = new Transform3D(new Basis(side, along, side.Cross(along)), (from + to) / 2f);

        Tween tween = beam.CreateTween();
        tween.TweenProperty(material, "albedo_color:a", 0f, 0.35f);
        tween.TweenCallback(Callable.From(beam.QueueFree));
    }

    // An expanding, fading shell where the pulse went off. A placeholder look.
    private void ShowEmpPulse(Vector3 at)
    {
        if (_world == null)
        {
            return;
        }

        StandardMaterial3D material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            AlbedoColor = new Color(0.4f, 0.8f, 1f, 0.45f),
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        };
        MeshInstance3D shell = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 1f, Height = 2f, Material = material },
            Position = at + new Vector3(0f, 1f, 0f),
            Scale = Vector3.One * 0.3f,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        _world.AddChild(shell);

        Tween tween = shell.CreateTween().SetParallel(true);
        tween.TweenProperty(shell, "scale", Vector3.One * 10f, 0.45f);
        tween.TweenProperty(material, "albedo_color:a", 0f, 0.45f);
        tween.Chain().TweenCallback(Callable.From(shell.QueueFree));
    }

    private void OnDirectReceived(string partnerId, string partnerName, string text, bool incoming)
    {
        GD.Print("Chat: " + (incoming ? "[From " : "[To ") + partnerName + "] " + text);
        _chat?.AddDirect(partnerId, partnerName, text, incoming);
        ChatLine line = new ChatLine(partnerName, (incoming ? "[From " : "[To ") + partnerName + "] " + text, ChatKind.Direct);
        _chatLog.Add(line);

        if (_chatLog.Count > ChatKept)
        {
            _chatLog.RemoveAt(0);
        }

        _terminal?.AddChatLine(line);
    }

    private void OnChatReceived(string sender, string text, int kind)
    {
        GD.Print("Chat: " + (sender.Length > 0 ? sender + ": " : "") + text);
        _chat?.AddLine(sender, text, (ChatKind)kind);

        ChatLine line = new ChatLine(sender, text, (ChatKind)kind);
        _chatLog.Add(line);

        if (_chatLog.Count > ChatKept)
        {
            _chatLog.RemoveAt(0);
        }

        _terminal?.AddChatLine(line);
    }

    private void OnTerminalOpened(int terminalType, string terminalName)
    {
        GD.Print("Online at " + terminalName);
        CloseTerminal();
        ClosePanels();

        _terminal = TerminalScene.Instantiate<TerminalScreen>();
        _ui.AddChild(_terminal);
        _terminal.Open((TerminalType)terminalType, terminalName, _chatLog);
        _terminal.GoOfflinePressed += _terminalNetwork.SendLeave;
        _terminal.ChatSubmitted += _network.SendChat;
        _terminal.TakeJobPressed += _terminalNetwork.SendTakeJob;
        _terminal.DroneReported += _terminalNetwork.SendSpot;
        _terminal.DefenseStartPressed += _terminalNetwork.SendDefenseStart;
        _terminal.DefenseFinished += _terminalNetwork.SendDefenseFinish;
        _terminal.CrackStartPressed += _terminalNetwork.SendCrackStart;
        _terminal.CrackGuessSubmitted += _terminalNetwork.SendCrackGuess;
        _terminal.WhoisSearchSubmitted += _socialNetwork.SendWhoisSearch;
        _terminal.WhoisOpenPressed += _socialNetwork.SendWhoisOpen;
        _terminal.WhoisMinePressed += () =>
        {
            Players.Player? self = GetTree().GetFirstNodeInGroup(Players.Player.LocalGroup) as Players.Player;

            if (self != null)
            {
                _socialNetwork.SendWhoisOpen(self.PlayerIdText);
            }
        };
        _terminal.WhoisPropsPressed += _socialNetwork.SendWhoisProps;
        _terminal.WhoisEditSubmitted += _socialNetwork.SendWhoisEdit;
        _terminal.WhoisFriendPressed += _socialNetwork.SendBefriendId;

        // All talk is in the chat: a message from Whois leaves the terminal and opens the
        // conversation there.
        _terminal.WhoisMessagePressed += (id, name) =>
        {
            _terminalNetwork.SendLeave();
            _chat?.OpenDirect(id, name);
        };

        if (_finder != null)
        {
            _finder.Paused = true;
        }

        _hud?.ShowPrompt("");
    }

    private void CloseTerminal()
    {
        if (_terminal == null)
        {
            return;
        }

        GD.Print("Offline");
        _terminal.QueueFree();
        _terminal = null;

        if (_finder != null)
        {
            _finder.Paused = false;
        }
    }

    private void OnNoticeReceived(string text)
    {
        GD.Print("Notice: " + text);
        _hud?.ShowNotice(text);
        _terminal?.ShowNotice(text);
    }

    private void ToggleInventory()
    {
        if (_inventoryPanel != null)
        {
            CloseInventory();
            return;
        }

        ClosePanels();
        _inventoryPanel = InventoryScene.Instantiate<InventoryPanel>();
        _ui.AddChild(_inventoryPanel);
        _inventoryPanel.ShowBag(_stacks, _dollars, _instances);
        _inventoryPanel.EquipPressed += id => _itemNetwork.SendEquip(id.ToString());
        _inventoryPanel.DropPressed += (type, tier, quantity) =>
            _intents?.Start("drop", id => _itemNetwork.SendDrop(id, (int)type, (int)tier, quantity));
        _inventoryPanel.UnequipPressed += id => _itemNetwork.SendUnequip(id.ToString());
        _inventoryPanel.RepairPackPressed += _itemNetwork.SendOpenRepairPack;
        _inventoryPanel.ShowRepairPack(_career == (int)Rules.Skills.CareerId.MechanicalEngineer);
    }

    private void OnMapReceived(string zoneId, byte[] cells)
    {
        _maps[zoneId] = cells;

        if (_map != null && zoneId == _zoneId)
        {
            _map.ShowCells(cells);
        }
    }

    private void OnContactsReceived(string[] friendIds, string[] friendNames, string[] friendZones, string[] ignoredIds, string[] ignoredNames)
    {
        _contacts = new string[][] { friendIds, friendNames, friendZones, ignoredIds, ignoredNames };
        _social?.ShowContacts(friendIds, friendNames, friendZones, ignoredIds, ignoredNames);
    }

    private void OnProgressReceived(int[] skills, long[] xp, int career, long careerXp, int rank, bool classTaken, int level)
    {
        _skillIds = skills;
        _skillXp = xp;
        _career = career;
        _careerXp = careerXp;
        _careerRank = rank;
        _classTaken = classTaken;
        _level = level;
        _skills?.ShowProgress(skills, xp, career, careerXp, rank, classTaken, level);
        _college?.ShowProgress(skills, xp, career, careerXp, rank, classTaken);
        _inventoryPanel?.ShowRepairPack(career == (int)Rules.Skills.CareerId.MechanicalEngineer);
    }

    private void OnCollegeOpened(string role)
    {
        ClosePanels();
        Node3D? self = GetTree().GetFirstNodeInGroup(Players.Player.LocalGroup) as Node3D;
        _panelOpenedAt = self != null ? self.GlobalPosition : Vector3.Zero;
        _college = CollegeScene.Instantiate<CollegePanel>();
        _ui.AddChild(_college);
        _college.Open(role);
        _college.ShowProgress(_skillIds, _skillXp, _career, _careerXp, _careerRank, _classTaken);
        _college.ClassPressed += _progressNetwork.SendTakeClass;
        _college.EnrollPressed += _progressNetwork.SendEnroll;
        _college.RankUpPressed += _progressNetwork.SendRankUp;
    }

    private void CloseCollege()
    {
        if (_college != null)
        {
            _college.QueueFree();
            _college = null;
        }
    }

    private void ToggleSkills()
    {
        if (_skills != null)
        {
            _skills.QueueFree();
            _skills = null;
            return;
        }

        ClosePanels();
        _skills = SkillsScene.Instantiate<SkillsPanel>();
        _ui.AddChild(_skills);
        _skills.ShowProgress(_skillIds, _skillXp, _career, _careerXp, _careerRank, _classTaken, _level);
        _skills.ShowAchievements(_achievements);
    }

    private void CloseSkills()
    {
        if (_skills != null)
        {
            _skills.QueueFree();
            _skills = null;
        }
    }

    private void CloseSocial()
    {
        if (_social != null)
        {
            _social.QueueFree();
            _social = null;
        }
    }

    private void ToggleSocial()
    {
        if (_social != null)
        {
            _social.QueueFree();
            _social = null;
            return;
        }

        ClosePanels();
        _social = SocialScene.Instantiate<SocialPanel>();
        _ui.AddChild(_social);
        _social.ShowContacts(_contacts[0], _contacts[1], _contacts[2], _contacts[3], _contacts[4]);
        _social.RemovePressed += _socialNetwork.SendRemove;
        _social.MessagePressed += (id, name) => _chat?.OpenDirect(id, name);
    }

    private void ToggleMap()
    {
        if (_map != null)
        {
            CloseMap();
            return;
        }

        Zone? zone = _world?.GetZone(_zoneId);

        if (zone == null || zone.MapSize == Vector2.Zero)
        {
            OnNoticeReceived("There is no map of this place.");
            return;
        }

        ClosePanels();
        byte[]? cells;
        _maps.TryGetValue(_zoneId, out cells);
        _map = MapScene.Instantiate<MapPanel>();
        _ui.AddChild(_map);
        _map.Open(ZoneIds.DisplayName(_zoneId), zone.MapSize, cells);
    }

    // One panel at a time: opening one closes whichever was open.
    private void ClosePanels()
    {
        CloseInventory();
        CloseShop();
        CloseWorkbench();
        CloseGive();
        CloseRecycler();
        CloseCollege();
        CloseSkills();
        CloseSocial();
        CloseMap();
        ClosePlantCard();
        CloseBook();
    }

    // The subway's visitor book, a page at a time; opened by the first page to arrive.
    private void ShowBook(int page, int pages, string[] lines)
    {
        GD.Print("Visitor book: page " + (page + 1) + " of " + pages + ", " + lines.Length + " names");

        if (_book == null)
        {
            ClosePanels();
            _panelOpenedAt = SelfPosition();
            _book = new VisitorBookPanel();
            _ui.AddChild(_book);
            _book.PagePressed += _subwayNetwork.SendReadBook;
            _book.Closed += CloseBook;
        }

        _book.ShowPage(page, pages, lines);
    }

    private void CloseBook()
    {
        if (_book != null)
        {
            _book.QueueFree();
            _book = null;
        }
    }

    private void CloseInventory()
    {
        if (_inventoryPanel != null)
        {
            _inventoryPanel.QueueFree();
            _inventoryPanel = null;
        }
    }

    private void CloseMap()
    {
        if (_map != null)
        {
            _map.QueueFree();
            _map = null;
        }
    }

    private void OpenInGameMenu()
    {
        _inGameMenu = InGameMenuScene.Instantiate<InGameMenu>();
        _ui.AddChild(_inGameMenu);
        _inGameMenu.ResumePressed += CloseInGameMenu;
        _inGameMenu.SettingsPressed += OpenSettings;
        _inGameMenu.WardrobePressed += OpenWardrobe;
        _inGameMenu.LeavePressed += Leave;
        _inGameMenu.QuitPressed += Quit;
    }

    private void CloseInGameMenu()
    {
        if (_inGameMenu != null)
        {
            _inGameMenu.QueueFree();
            _inGameMenu = null;
        }
    }

    private void OpenSettings()
    {
        SettingsPanel settings = SettingsScene.Instantiate<SettingsPanel>();
        _ui.AddChild(settings);
        settings.Open(_settings);
        settings.Changed += ApplyControls;
    }

    // The settings that act in the world: the camera, and the keys the HUD names.
    private void ApplyControls()
    {
        if (_world == null || _hud == null)
        {
            return;
        }

        Players.ChaseCamera camera = _world.GetNode<Players.ChaseCamera>("Camera");
        camera.Sensitivity = _settings.MouseSensitivity;
        camera.Distance = _settings.CameraDistance;
        _hud.UseKey = ClientSettings.KeyName("interact");
        ShowBattery();
        _hud.ShowHint(
            ClientSettings.KeyName("move_forward") + "/" + ClientSettings.KeyName("move_back") + " walk   "
            + ClientSettings.KeyName("turn_left") + "/" + ClientSettings.KeyName("turn_right") + " turn   "
            + ClientSettings.KeyName("strafe_left") + "/" + ClientSettings.KeyName("strafe_right") + " step   "
            + ClientSettings.KeyName("jump") + " jump   Right-drag look   Wheel zoom   Click a player   "
            + ClientSettings.KeyName("interact") + " use   "
            + ClientSettings.KeyName("emp") + " EMP   Enter chat");
    }

    private void Leave()
    {
        LeaveWorld();
        Disconnect();
        ShowMainMenu("");
    }

    private void LeaveWorld()
    {
        CloseInGameMenu();

        // The wheel zoom is kept for next time.
        _settings.Save();

        if (_bot != null)
        {
            _bot.QueueFree();
            _bot = null;
        }

        if (_hud != null)
        {
            _hud.QueueFree();
            _hud = null;
        }

        if (_inventoryPanel != null)
        {
            _inventoryPanel.QueueFree();
            _inventoryPanel = null;
        }

        if (_chat != null)
        {
            _chat.QueueFree();
            _chat = null;
        }

        if (_party != null)
        {
            _party.QueueFree();
            _party = null;
        }

        if (_finder != null)
        {
            _finder.QueueFree();
            _finder = null;
        }

        if (_terminal != null)
        {
            _terminal.QueueFree();
            _terminal = null;
        }

        if (_intents != null)
        {
            _intents.QueueFree();
            _intents = null;
        }

        CloseShop();
        CloseWorkbench();
        CloseGive();
        CloseMap();
        _maps.Clear();
        CloseSocial();
        CloseSkills();
        CloseCollege();
        CloseRecycler();
        CloseGarden();
        ClosePlantCard();

        _instances = new List<ItemInstance>();
        _chatLog.Clear();

        _stacks = new List<ItemStack>();

        // Out of the tree at once, not at the end of the frame: its nodes must not run
        // another frame against a connection that is already gone.
        if (_world != null)
        {
            _main.RemoveChild(_world);
            _world.QueueFree();
            _world = null;
        }
    }

    private void Disconnect()
    {
        if (Multiplayer.MultiplayerPeer != null)
        {
            Multiplayer.MultiplayerPeer.Close();
        }

        Multiplayer.MultiplayerPeer = null;
    }

    private void Quit()
    {
        _settings.Save();
        Disconnect();
        GetTree().Quit();
    }
}
