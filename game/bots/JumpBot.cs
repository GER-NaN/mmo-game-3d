namespace MmoGame3d.Bots;

using System.IO;
using Godot;
using MmoGame3d.Players;

/// <summary>
/// A bot that waits until its player is in the world, presses jump once, and sees the
/// server's position of its body rise. Needs a connected client (--autoconnect; the
/// Overseer's --connect).
/// </summary>
public partial class JumpBot : Node
{
    private const double LookInterval = 0.25;

    // Time to land after arriving, before the jump.
    private const double SettleSeconds = 2;

    // How long the rise may take to show, and how high it must be.
    private const double RiseLimitSeconds = 3;
    private const float MinRise = 0.5f;

    private string _folder = "";
    private BotEventLog? _events;
    private double _sinceLook;
    private Stage _stage = Stage.WaitForWorld;
    private double _stageTime;
    private Player? _player;
    private float _groundY;
    private float _highestY;

    private enum Stage
    {
        WaitForWorld,
        Settle,
        Release,
        WatchRise,
        Done,
    }

    public override void _Ready()
    {
        _folder = FolderFromCommandLine();

        if (_folder.Length == 0)
        {
            GD.PushError("JumpBot: no --bot-folder given; the bot does nothing.");
            SetProcess(false);
            return;
        }

        _events = new BotEventLog(_folder);
        _events.Write("started", "");
    }

    public override void _Process(double delta)
    {
        _stageTime += delta;

        // The rise is watched every frame, to catch the top of the jump.
        if (_stage == Stage.WatchRise && _player != null && IsInstanceValid(_player))
        {
            _highestY = Mathf.Max(_highestY, _player.NetPosition.Y);
        }

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
            case Stage.WaitForWorld:
                _player = GetTree().GetFirstNodeInGroup(Player.LocalGroup) as Player;

                if (_player != null)
                {
                    _events!.Write("in-world", "");
                    Next(Stage.Settle);
                }

                break;
            case Stage.Settle:
                if (_stageTime >= SettleSeconds)
                {
                    _groundY = _player!.NetPosition.Y;
                    _highestY = _groundY;
                    PressJump(true);
                    _events!.Write("pressed", "jump");
                    Next(Stage.Release);
                }

                break;
            case Stage.Release:
                PressJump(false);
                Next(Stage.WatchRise);
                break;
            case Stage.WatchRise:
                float rise = _highestY - _groundY;

                if (rise >= MinRise)
                {
                    _events!.Write("jumped", rise.ToString("0.00") + " m");
                    _events.Write("done", "");
                    Next(Stage.Done);
                }
                else if (_stageTime > RiseLimitSeconds)
                {
                    _events!.Write("failed", "rose only " + rise.ToString("0.00") + " m");
                    Next(Stage.Done);
                }

                break;
            case Stage.Done:
                break;
        }
    }

    private void Next(Stage stage)
    {
        _stage = stage;
        _stageTime = 0;
    }

    // The same event a key bound to jump makes, fed through the input system.
    private static void PressJump(bool pressed)
    {
        InputEventAction jump = new InputEventAction();
        jump.Action = "jump";
        jump.Pressed = pressed;
        Input.ParseInputEvent(jump);
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
}
