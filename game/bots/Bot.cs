namespace MmoGame3d.Bots;

using System.IO;
using Godot;
using MmoGame3d.Ui;

/// <summary>
/// One bot, inside one client. It is the one child the bot scene adds under Main, so the
/// game itself never knows it is there. For now it only waits for the main menu, reports
/// it, and quits when the Overseer leaves a stop file in its execution folder.
/// </summary>
public partial class Bot : Node
{
    public const string StopFile = "stop";

    // Looking is a walk over the whole tree, so not every frame.
    private const double LookInterval = 0.25;

    private string _folder = "";
    private BotEventLog? _events;
    private double _sinceLook;
    private bool _sawMainMenu;

    public override void _Ready()
    {
        _folder = FolderFromCommandLine();

        if (_folder.Length == 0)
        {
            GD.PushError("Bot: no --bot-folder given; the bot does nothing.");
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

        if (File.Exists(Path.Combine(_folder, StopFile)))
        {
            _events!.Write("stopped", "");
            _events.Close();
            SetProcess(false);
            GetTree().Quit();
            return;
        }

        if (!_sawMainMenu && FindMainMenu(GetTree().Root) != null)
        {
            _sawMainMenu = true;
            _events!.Write("main-menu", "");
            _events.Write("done", "");
        }
    }

    // LaunchOptions ignores options it does not know, so the bot's ride on the same line.
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
