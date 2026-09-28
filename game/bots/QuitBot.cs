namespace MmoGame3d.Bots;

using System.IO;
using Godot;
using MmoGame3d.Ui;

/// <summary>
/// A bot that waits for the main menu and clicks Quit, as a player would. The game
/// quitting is the success: the click quits at the end of the frame, before the stop
/// file that "done" brings. If the stop file is seen, the click did not quit the game,
/// and this quits with exit code 1 so the run fails.
/// </summary>
public partial class QuitBot : Node
{
    // Looking is a walk over the whole tree, so not every frame.
    private const double LookInterval = 0.25;

    private string _folder = "";
    private BotEventLog? _events;
    private double _sinceLook;
    private bool _clicked;

    public override void _Ready()
    {
        _folder = FolderFromCommandLine();

        if (_folder.Length == 0)
        {
            GD.PushError("QuitBot: no --bot-folder given; the bot does nothing.");
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
            _events!.Write("stopped", "the game did not quit");
            _events.Close();
            SetProcess(false);
            GetTree().Quit(1);
            return;
        }

        if (_clicked)
        {
            return;
        }

        MainMenu? menu = FindMainMenu(GetTree().Root);

        if (menu == null)
        {
            return;
        }

        _events!.Write("main-menu", "");
        Click(menu.GetNode<Button>("%Quit"));
        _clicked = true;
        _events.Write("clicked", "Quit");
        _events.Write("done", "");
    }

    // A real press and release at the button's centre, through the viewport, so the click
    // goes the way a player's does: a hidden, disabled or covered button does not press.
    private void Click(Control control)
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

    private static MainMenu? FindMainMenu(Node node)
    {
        MainMenu? menu = node as MainMenu;

        if (menu != null && menu.IsVisibleInTree())
        {
            return menu;
        }

        foreach (Node child in node.GetChildren())
        {
            MainMenu? found = FindMainMenu(child);

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
