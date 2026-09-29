namespace MmoGame3d.Bots;

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

/// <summary>
/// The bot: the one node the bot scene adds under Main, so the game itself never knows
/// it is there (bots.md T6). It reads its execution folder's bot.json for the activity
/// to run, runs the activity's steps one after another, and reports each step, then
/// "done" or "failed", in the events file. A step past its time limit fails. The stop
/// file ends the run: exit code 0 if the plan finished, 1 if not.
/// </summary>
public partial class BotRunner : Node
{
    public const string StopFile = "stop";

    private const double StopLookInterval = 0.25;

    private readonly List<BotStep> _steps = new List<BotStep>();
    private string _folder = "";
    private BotEventLog? _events;
    private BotBody? _body;
    private BotChatter _chatter = new BotChatter(new string[0]);
    private int _current;
    private bool _started;
    private double _stepTime;
    private double _sinceStopLook;
    private bool _finished;
    private bool _failed;

    public override void _Ready()
    {
        _folder = FolderFromCommandLine();

        if (_folder.Length == 0)
        {
            GD.PushError("BotRunner: no --bot-folder given; the bot does nothing.");
            SetProcess(false);
            return;
        }

        _events = new BotEventLog(_folder);
        _body = new BotBody(this, _events, InsertNext);
        string name = ActivityName();
        BotActivity? activity = BotActivities.Named(name);
        _events.Write("started", name);

        if (activity == null)
        {
            _events.Write("failed", "no activity named \"" + name + "\"");
            _finished = true;
            _failed = true;
            return;
        }

        _steps.AddRange(activity.Steps());
        _chatter = new BotChatter(activity.Phrases);
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

        if (!_finished)
        {
            _chatter.Tick(_body!, this, delta);
        }

        // A step that finishes hands over to the next in the same frame, so nothing the
        // game does at the frame's end (quitting) comes between them.
        double stepDelta = delta;

        while (!_finished)
        {
            if (_current == _steps.Count)
            {
                _events!.Write("done", "");
                _finished = true;
                return;
            }

            BotStep step = _steps[_current];

            if (!_started)
            {
                _events!.Write("step", step.Name);
                _started = true;
                _stepTime = 0;
                step.Start(_body!);
            }

            _stepTime += stepDelta;
            BotStepState state = step.Tick(_body!, stepDelta);
            stepDelta = 0;

            if (state == BotStepState.Running && _stepTime > step.TimeLimit)
            {
                state = BotStepState.Failed;
                _events!.Write("failed", step.Name + ": not done after " + step.TimeLimit + " s");
            }
            else if (state == BotStepState.Failed)
            {
                _events!.Write("failed", step.Name + ": " + step.FailReason);
            }

            if (state == BotStepState.Running)
            {
                return;
            }

            step.End(_body!);
            _started = false;

            if (state == BotStepState.Failed)
            {
                _body!.ReleaseAll();
                RecordFailure();
                _finished = true;
                _failed = true;
                return;
            }

            _current++;
        }
    }

    // Where it failed, and what the window showed then (bots.md F3). A headless client
    // draws nothing, so it has no picture.
    private void RecordFailure()
    {
        Players.Player? player = _body!.Player;
        Zones.Zone? zone = _body.Zone;

        if (player != null && zone != null)
        {
            Vector3 at = zone.ToLocal(player.GlobalPosition);
            _events!.Write("position", zone.ZoneId + " (" + at.X.ToString("0.0") + ", " + at.Y.ToString("0.0") + ", " + at.Z.ToString("0.0") + ")");
        }

        if (DisplayServer.GetName() != "headless")
        {
            string picture = Path.Combine(_folder, "failed.png");
            GetViewport().GetTexture().GetImage().SavePng(picture);
            _events!.Write("screenshot", "failed.png");
        }
    }

    // A need met by another activity: its steps run next.
    private void InsertNext(List<BotStep> steps)
    {
        _steps.InsertRange(_current + 1, steps);
    }

    private void Stop()
    {
        bool planDone = _finished && !_failed;

        if (_started)
        {
            _steps[_current].End(_body!);
        }

        _body!.ReleaseAll();
        _events!.Write("stopped", "");
        _events.Close();
        SetProcess(false);
        GetTree().Quit(planDone ? 0 : 1);
    }

    private string ActivityName()
    {
        string path = Path.Combine(_folder, "bot.json");

        if (!File.Exists(path))
        {
            return "";
        }

        using (JsonDocument json = JsonDocument.Parse(File.ReadAllText(path)))
        {
            JsonElement activity;
            return json.RootElement.TryGetProperty("activity", out activity) ? activity.GetString() ?? "" : "";
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
}
