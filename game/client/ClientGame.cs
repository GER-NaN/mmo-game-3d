namespace MmoGame3d.Client;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Dev;
using MmoGame3d.Networking;
using MmoGame3d.Rules;
using MmoGame3d.Rules.Chat;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Players;
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
    private static readonly PackedScene WorldScene = GD.Load<PackedScene>("res://game/zones/World.tscn");

    private LaunchOptions _options = null!;
    private Network _network = null!;
    private PartyNetwork _partyNetwork = null!;
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
    private BotDriver? _bot;

    // What the server last said this player carries.
    private List<ItemStack> _stacks = new List<ItemStack>();
    private string _pendingName = "";
    private string _address = "";

    public void Start(LaunchOptions options, Network network, PartyNetwork partyNetwork, Node main)
    {
        _options = options;
        _network = network;
        _partyNetwork = partyNetwork;
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
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_world == null)
        {
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
        _world.LoadZone(zoneId);

        _hud = HudScene.Instantiate<Hud>();
        _ui.AddChild(_hud);
        _hud.ShowIdentity(displayName, zoneId);

        _chat = ChatScene.Instantiate<ChatBox>();
        _ui.AddChild(_chat);
        _chat.Submitted += OnChatSubmitted;

        _party = new ClientParty { Name = "Party" };
        AddChild(_party);
        _party.Start(_partyNetwork, _ui, _world);

        _network.SendWorldReady();

        if (_options.Bot)
        {
            _bot = new BotDriver { Name = "Bot", Say = _network.SendChat };
            AddChild(_bot);
        }
    }

    private void OnInventoryReceived(int[] packed)
    {
        _stacks = InventoryWire.Unpack(packed);
        _inventoryPanel?.ShowStacks(_stacks);
    }

    private void OnClockReceived(double secondsOfDay)
    {
        _world?.GetNode<DayNight>("DayNight").SetTime(secondsOfDay);
    }

    // "/p " speaks to the party; anything else to everyone.
    private void OnChatSubmitted(string text)
    {
        if (text.StartsWith("/p ", System.StringComparison.OrdinalIgnoreCase) && _party != null)
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
        _inventoryPanel.ShowStacks(_stacks);
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
