namespace MmoGame3d.Client;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Dev;
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

    // Walking this far from where a shop or a workbench was opened closes it.
    private const float PanelWalkAway = 4f;

    // How much chat the client keeps, for a screen opened later.
    private const int ChatKept = 100;
    private static readonly PackedScene WorldScene = GD.Load<PackedScene>("res://game/zones/World.tscn");

    private LaunchOptions _options = null!;
    private Network _network = null!;
    private PartyNetwork _partyNetwork = null!;
    private TerminalNetwork _terminalNetwork = null!;
    private ShopNetwork _shopNetwork = null!;
    private ItemNetwork _itemNetwork = null!;
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
    private Players.Player? _giveTo;
    private Vector3 _panelOpenedAt;
    private List<ItemInstance> _instances = new List<ItemInstance>();
    private ClientIntents? _intents;
    private int _dollars;
    private readonly List<ChatLine> _chatLog = new List<ChatLine>();
    private BotDriver? _bot;

    // What the server last said this player carries.
    private List<ItemStack> _stacks = new List<ItemStack>();
    private string _pendingName = "";
    private string _zoneId = "";
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
        _network.ClockReceived += OnClockReceived;
        _network.ZoneChanged += zoneId => Callable.From(() => OnZoneChanged(zoneId)).CallDeferred();
        _shopNetwork.ShopOpened += OnShopOpened;
        _itemNetwork.WorkbenchOpened += OnWorkbenchOpened;
        _network.IntentAnswered += (id, refusal) => _intents?.Answer(id, refusal);
        _terminalNetwork.Opened += OnTerminalOpened;
        _terminalNetwork.Closed += CloseTerminal;
        _terminalNetwork.RosterReceived += (names, zones, online) => _terminal?.ShowRoster(names, zones, online);
        _terminalNetwork.TownReceived += (working, taken, log) => _terminal?.ShowTown(working, taken, log);
        // Deferred, like the login answers above: these fire inside the engine's network
        // poll, and closing the peer or freeing the world is better done after it.
        Multiplayer.ConnectedToServer += () => Callable.From(OnConnected).CallDeferred();
        Multiplayer.ConnectionFailed += () => Callable.From(OnConnectionFailed).CallDeferred();
        Multiplayer.ServerDisconnected += () => Callable.From(OnServerDisconnected).CallDeferred();

        if (options.AutoConnect)
        {
            Connect(DefaultName(), options.Address ?? _settings.Address);
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

        if ((_shop != null || _workbench != null || _give != null) && self != null && self.GlobalPosition.DistanceTo(_panelOpenedAt) > PanelWalkAway)
        {
            CloseShop();
            CloseWorkbench();
            CloseGive();
        }

        // The one being given to walked off, left, or changed zone.
        if (_give != null && (_giveTo == null || !IsInstanceValid(_giveTo)))
        {
            CloseGive();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_world == null)
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

        if ((_shop != null || _workbench != null || _give != null) && @event.IsActionPressed("ui_cancel"))
        {
            GetViewport().SetInputAsHandled();
            CloseShop();
            CloseWorkbench();
            CloseGive();
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

        _menu.Fill(_profile.Name, DefaultName(), _settings.Address);
        _menu.SetStatus(status);
        _menu.SetBusy(false);
    }

    private void Connect(string name, string address)
    {
        string? problem = DisplayName.Problem(name);

        if (problem != null)
        {
            ShowMainMenu(problem);
            return;
        }

        _pendingName = name.Trim();
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
        GD.Print("Connected as peer " + Multiplayer.GetUniqueId() + "; logging in");
        _network.SendLogin(GameVersion.Protocol, _profile.LicenseKey().ToString(), _pendingName);
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

        _world = WorldScene.Instantiate<World>();
        _world.Name = "World";
        _main.AddChild(_world);
        _world.LoadZone(zoneId, Vector3.Zero);
        _zoneId = zoneId;
        _displayName = displayName;

        _hud = HudScene.Instantiate<Hud>();
        _ui.AddChild(_hud);
        _hud.ShowIdentity(displayName, ZoneIds.SceneOf(zoneId));

        _chat = ChatScene.Instantiate<ChatBox>();
        _ui.AddChild(_chat);
        _chat.Submitted += OnChatSubmitted;

        _party = new ClientParty { Name = "Party" };
        AddChild(_party);
        _party.Start(_partyNetwork, _ui, _world);
        _party.GiveRequested += OpenGive;

        _intents = new ClientIntents { Name = "Intents" };
        AddChild(_intents);
        _intents.Answered += (label, refusal) =>
        {
            if (refusal.Length > 0)
            {
                OnNoticeReceived(refusal);
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
    }

    private void OnWorkbenchOpened()
    {
        CloseWorkbench();
        CloseShop();
        _panelOpenedAt = SelfPosition();
        _workbench = WorkbenchScene.Instantiate<WorkbenchPanel>();
        _ui.AddChild(_workbench);
        _workbench.ShowBench(_stacks, _instances);
        _workbench.Closed += CloseWorkbench;
        _workbench.RemovePressed += id => _itemNetwork.SendRemoveBattery(id.ToString());
        _workbench.InsertPressed += id => _itemNetwork.SendInsertBattery(id.ToString());
    }

    private void OpenGive(Players.Player target)
    {
        CloseGive();
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
        CloseShop();
        CloseWorkbench();
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
            _hud?.ShowIdentity(_displayName, ZoneIds.SceneOf(zoneId));
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

        _terminal = TerminalScene.Instantiate<TerminalScreen>();
        _ui.AddChild(_terminal);
        _terminal.Open((TerminalType)terminalType, terminalName, _chatLog);
        _terminal.GoOfflinePressed += _terminalNetwork.SendLeave;
        _terminal.ChatSubmitted += _network.SendChat;
        _terminal.TakeJobPressed += () => _terminalNetwork.SendTakeJob(StreetLights.JobId);

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
    }

    private void ToggleInventory()
    {
        if (_inventoryPanel != null)
        {
            _inventoryPanel.QueueFree();
            _inventoryPanel = null;
            return;
        }

        _inventoryPanel = InventoryScene.Instantiate<InventoryPanel>();
        _ui.AddChild(_inventoryPanel);
        _inventoryPanel.ShowBag(_stacks, _dollars, _instances);
        _inventoryPanel.EquipPressed += id => _itemNetwork.SendEquip(id.ToString());
        _inventoryPanel.DropPressed += (type, tier, quantity) =>
            _intents?.Start("drop", id => _itemNetwork.SendDrop(id, (int)type, (int)tier, quantity));
        _inventoryPanel.UnequipPressed += id => _itemNetwork.SendUnequip(id.ToString());
    }

    private void OpenInGameMenu()
    {
        _inGameMenu = InGameMenuScene.Instantiate<InGameMenu>();
        _ui.AddChild(_inGameMenu);
        _inGameMenu.ResumePressed += CloseInGameMenu;
        _inGameMenu.SettingsPressed += OpenSettings;
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
        Disconnect();
        GetTree().Quit();
    }
}
