namespace MmoGame3d.Client;

using Godot;
using MmoGame3d.Dev;
using MmoGame3d.Networking;
using MmoGame3d.Rules;
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
    private static readonly PackedScene WorldScene = GD.Load<PackedScene>("res://game/zones/World.tscn");

    private LaunchOptions _options = null!;
    private Network _network = null!;
    private Node _main = null!;
    private ClientSettings _settings = null!;
    private Profile _profile = null!;
    private CanvasLayer _ui = null!;

    private MainMenu? _menu;
    private InGameMenu? _inGameMenu;
    private World? _world;
    private Hud? _hud;
    private BotDriver? _bot;
    private string _pendingName = "";
    private string _address = "";

    public void Start(LaunchOptions options, Network network, Node main)
    {
        _options = options;
        _network = network;
        _main = main;
        _profile = new Profile(options.Profile);
        _settings = ClientSettings.Load();

        if (DisplayServer.GetName() != "headless")
        {
            _settings.Apply();
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

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_world == null || !@event.IsActionPressed("ui_cancel"))
        {
            return;
        }

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

        _network.SendWorldReady();

        if (_options.Bot)
        {
            _bot = new BotDriver { Name = "Bot" };
            AddChild(_bot);
        }
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
