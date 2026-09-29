namespace MmoGame3d.Bots;

using System.IO;
using Godot;

/// <summary>
/// The bot: the one node the bot scene adds under Main, so the game itself never knows
/// it is there (bots.md T6). From its execution folder's bot.json it takes a persona, or
/// one activity to play once. It plays one activity at a time, picking the next when
/// free, while its watchers look on and its chatter talks now and then.
///
/// Events: "activity" as one starts; "completed" or "failed" as it ends ("walked-away"
/// if the persona leaves it); "finding" from a watcher; "done" when a one-activity bot has
/// played its activity well. The stop file ends the run: exit code 1 for a one-activity
/// bot that did not finish well, 0 otherwise.
/// </summary>
public partial class BotRunner : Node
{
    public const string StopFile = "stop";

    private const double StopLookInterval = 0.25;

    private readonly BotWatch _watch = new BotWatch();
    private readonly BotChatter _chatter = new BotChatter();
    private string _folder = "";
    private BotEventLog? _events;
    private BotBody? _body;
    private BotPersona? _persona;
    private BotActivityRun? _run;
    private double _sinceStopLook;
    private bool _finished;
    private bool _onceOnly;
    private bool _allWell = true;
    private int _failures;

    public override void _Ready()
    {
        _folder = FolderFromCommandLine();

        if (_folder.Length == 0)
        {
            GD.PushError("BotRunner: no --bot-folder given; the bot does nothing.");
            SetProcess(false);
            return;
        }

        BotSetup setup = BotSetup.Read(_folder);
        _events = new BotEventLog(_folder);
        _body = new BotBody(this, _folder, _events, setup.Seed);
        _onceOnly = setup.Persona.Length == 0;
        _persona = _onceOnly ? BotPersona.Only(setup.Activity) : BotPersonas.Named(setup.Persona);
        _events.Write("started", (_onceOnly ? setup.Activity : "persona " + setup.Persona) + ", seed " + setup.Seed);

        if (_persona == null)
        {
            Finish("no persona named \"" + setup.Persona + "\"");
        }
    }

    public override void _Process(double delta)
    {
        _sinceStopLook += delta;

        if (_sinceStopLook >= StopLookInterval)
        {
            _sinceStopLook = 0;

            if (File.Exists(Path.Combine(_folder, StopFile)))
            {
                Stop();
                return;
            }
        }

        if (_finished)
        {
            return;
        }

        BotBody body = _body!;
        _watch.Tick(body, _run?.Step, delta);

        if (_run == null && !StartNext())
        {
            return;
        }

        _chatter.Tick(body, _run!, delta);
        BotStepState state = _run!.Tick(body, delta);

        if (state == BotStepState.Running)
        {
            if (_run.AtBoundary && body.Random.NextDouble() < _persona!.WalkAwayChance)
            {
                _run.Cancel(body);
                _events!.Write("walked-away", "");
                EndRun();
            }

            return;
        }

        if (state == BotStepState.Done)
        {
            _events!.Write("completed", "");
        }
        else
        {
            _failures++;
            _allWell = false;
            _events!.Write("failed", _run.FailReason + " at " + body.Where());
            body.SavePicture("failed-" + _failures + ".png");
        }

        EndRun();
    }

    // The persona's next activity; false when there is none, and the bot is finished.
    private bool StartNext()
    {
        string? name = _persona!.Next(_body!.Random);

        if (name == null)
        {
            Finish(_allWell ? "" : "not all went well");
            return false;
        }

        BotActivity? activity = BotActivities.Named(name);

        if (activity == null)
        {
            _allWell = false;
            Finish("no activity named \"" + name + "\"");
            return false;
        }

        _run = new BotActivityRun(activity);
        _body.Run = _run;
        _events!.Activity = name;
        _events.Write("activity", name);
        return true;
    }

    private void EndRun()
    {
        _body!.ReleaseAll();
        _body.Navigator.Stop(_body);
        _body.Run = null;
        _body.NeedDepth = 0;
        _run = null;
        _events!.Activity = "";
    }

    // Nothing more to do: "done" when all went well, else why not.
    private void Finish(string problem)
    {
        _finished = true;

        if (problem.Length == 0)
        {
            _events!.Write("done", "");
        }
        else
        {
            _allWell = false;
            _events!.Write("failed", problem);
        }
    }

    private void Stop()
    {
        if (_run != null)
        {
            _run.Cancel(_body!);
        }

        _body!.ReleaseAll();
        _events!.Write("stopped", _watch.Findings + " findings");
        _events.Close();
        SetProcess(false);
        bool failedOnce = _onceOnly && !(_finished && _allWell);
        GetTree().Quit(failedOnce ? 1 : 0);
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
}
