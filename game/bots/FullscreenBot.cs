namespace MmoGame3d.Bots;

using System.IO;
using Godot;
using MmoGame3d.Ui;

/// <summary>
/// A bot that opens Settings from the main menu, turns Fullscreen on, sees the window go
/// fullscreen, turns it off again and sees the window come back.
/// </summary>
public partial class FullscreenBot : Node
{
    // Looking is a walk over the whole tree, so not every frame.
    private const double LookInterval = 0.25;

    private string _folder = "";
    private BotEventLog? _events;
    private double _sinceLook;
    private Stage _stage = Stage.WaitForMenu;
    private SettingsPanel? _settings;

    private enum Stage
    {
        WaitForMenu,
        WaitForSettings,
        WaitForFullscreen,
        WaitForWindowed,
        Done,
    }

    public override void _Ready()
    {
        _folder = FolderFromCommandLine();

        if (_folder.Length == 0)
        {
            GD.PushError("FullscreenBot: no --bot-folder given; the bot does nothing.");
            SetProcess(false);
            return;
        }

        _events = new BotEventLog(_folder);
        _events.Write("started", "");
    }

    public override void _Process(double delta)
    {
        _sinceLook += delta;

        if (_sinceLook < LookInterval)
        {
            return;
        }

        _sinceLook = 0;

        if (File.Exists(Path.Combine(_folder, Bot.StopFile)))
        {
            _events!.Write("stopped", "");
            _events.Close();
            SetProcess(false);
            GetTree().Quit();
            return;
        }

        switch (_stage)
        {
            case Stage.WaitForMenu:
                MainMenu? menu = Find<MainMenu>(GetTree().Root);

                if (menu != null)
                {
                    _events!.Write("main-menu", "");
                    Click(menu.GetNode<Button>("%Settings"));
                    _events.Write("clicked", "Settings");
                    _stage = Stage.WaitForSettings;
                }

                break;
            case Stage.WaitForSettings:
                _settings = Find<SettingsPanel>(GetTree().Root);

                if (_settings != null)
                {
                    _events!.Write("settings-open", "");
                    Click(_settings.GetNode<Button>("%Fullscreen"));
                    _events.Write("clicked", "Fullscreen");
                    _stage = Stage.WaitForFullscreen;
                }

                break;
            case Stage.WaitForFullscreen:
                if (IsFullscreen())
                {
                    _events!.Write("fullscreen", "on");
                    Click(_settings!.GetNode<Button>("%Fullscreen"));
                    _events.Write("clicked", "Fullscreen");
                    _stage = Stage.WaitForWindowed;
                }

                break;
            case Stage.WaitForWindowed:
                if (!IsFullscreen())
                {
                    _events!.Write("fullscreen", "off");
                    _events.Write("done", "");
                    _stage = Stage.Done;
                }

                break;
            case Stage.Done:
                break;
        }
    }

    private static bool IsFullscreen()
    {
        DisplayServer.WindowMode mode = DisplayServer.WindowGetMode();
        return mode == DisplayServer.WindowMode.Fullscreen || mode == DisplayServer.WindowMode.ExclusiveFullscreen;
    }

    // A real press and release at the control's centre, through the viewport, so the
    // click goes the way a player's does: a hidden, disabled or covered control does not
    // press.
    private static void Click(Control control)
    {
        Vector2 centre = control.GetGlobalTransformWithCanvas() * (control.Size / 2);
        Viewport viewport = control.GetViewport();

        InputEventMouseButton press = new InputEventMouseButton();
        press.ButtonIndex = MouseButton.Left;
        press.Position = centre;
        press.GlobalPosition = centre;
        press.Pressed = true;
        viewport.PushInput(press, true);

        InputEventMouseButton release = (InputEventMouseButton)press.Duplicate();
        release.Pressed = false;
        viewport.PushInput(release, true);
    }

    private static string FolderFromCommandLine()
    {
        string[] args = OS.GetCmdlineUserArgs();

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--bot-folder")
            {
                return args[i + 1];
            }
        }

        return "";
    }

    // The first visible control of this type anywhere in the tree.
    private static T? Find<T>(Node node)
        where T : Control
    {
        T? found = node as T;

        if (found != null && found.IsVisibleInTree())
        {
            return found;
        }

        foreach (Node child in node.GetChildren())
        {
            T? inChild = Find<T>(child);

            if (inChild != null)
            {
                return inChild;
            }
        }

        return null;
    }
}
