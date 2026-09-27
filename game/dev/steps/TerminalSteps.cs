namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Players;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Terminals;
using MmoGame3d.Rules.World;
using MmoGame3d.Ui;

/// <summary>
/// Online at a terminal or on the phone: looks at a few apps, a random one each time,
/// sometimes cracks a code or takes the repair job, then goes offline and checks that it
/// did.
/// </summary>
public sealed class OnlineStep : BotStep
{
    private static readonly string[] BrowsedApps =
    {
        TerminalApps.Chat, TerminalApps.Online, TerminalApps.Whois, TerminalApps.TodoList, TerminalApps.TownLog,
        TerminalApps.TownCameras, TerminalApps.StatusBoard, TerminalApps.ExchangeRate, TerminalApps.Defense,
    };

    private enum Phase
    {
        Pick,
        Read,
        Crack,
        Leave,
    }

    private Phase _phase;
    private int _appsLeft;
    private double _wait;
    private bool _jobClicked;
    private int _crackStep;
    private int _crackSeen;

    public OnlineStep()
        : base("be online", 180)
    {
    }

    public override void Begin(BotBody body)
    {
        _phase = Phase.Pick;
        _appsLeft = 1 + body.Random.Next(3);
        _wait = 0.8 * body.Pace;
        _jobClicked = false;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        if (!body.IsOnline)
        {
            return _phase == Phase.Leave ? StepResult.Done : StepResult.Failed;
        }

        _wait -= delta;

        if (_wait > 0)
        {
            return StepResult.Running;
        }

        switch (_phase)
        {
            case Phase.Pick:
                Pick(body);
                break;
            case Phase.Read:
                Button? take = body.Usable(TerminalScreen.TakeJobGroup);

                if (take != null && !_jobClicked)
                {
                    _jobClicked = true;
                    GD.Print("Bot: clicking Take the job");
                    body.Click(take);
                    _wait = 1;
                    break;
                }

                NextApp();
                break;
            case Phase.Crack:
                if (!Crack(body))
                {
                    NextApp();
                }

                break;
            default:
                body.CloseOne();
                _wait = 1.5;
                break;
        }

        return StepResult.Running;
    }

    private void NextApp()
    {
        _appsLeft--;
        _phase = _appsLeft > 0 ? Phase.Pick : Phase.Leave;
        _wait = 0.5;
    }

    private void Pick(BotBody body)
    {
        Button? cracker = body.Usable(TerminalScreen.AppGroupPrefix + TerminalApps.CodeCracker);

        if (cracker != null && body.Random.Next(3) == 0)
        {
            GD.Print("Bot: opening the code cracker");
            body.Click(cracker);
            _phase = Phase.Crack;
            _crackStep = 0;
            _wait = 0.6;
            return;
        }

        List<Button> apps = new List<Button>();

        foreach (string id in BrowsedApps)
        {
            Button? app = body.Usable(TerminalScreen.AppGroupPrefix + id);

            if (app != null)
            {
                apps.Add(app);
            }
        }

        if (apps.Count == 0)
        {
            _phase = Phase.Leave;
            return;
        }

        Button pick = apps[body.Random.Next(apps.Count)];
        GD.Print("Bot: opening " + pick.Text);
        body.Click(pick);
        _phase = Phase.Read;
        _wait = (2 + (body.Random.NextDouble() * 4)) * body.Pace;
    }

    // Start a code, then a guess each time a new answer shows, until cracked or locked.
    // False when finished.
    private bool Crack(BotBody body)
    {
        TerminalScreen? screen = body.Me?.GetTree().GetFirstNodeInGroup(TerminalScreen.GoOfflineGroup)?.Owner as TerminalScreen;
        _wait = 0.4;

        if (screen == null)
        {
            return false;
        }

        if (_crackStep == 0)
        {
            Button? start = body.Usable(TerminalScreen.CrackStartGroup);

            if (start == null)
            {
                return false;
            }

            body.Click(start);
            _crackSeen = -1;
            _crackStep = 1;
            return true;
        }

        string[]? guesses = screen.CrackGuesses;

        if (guesses == null || guesses.Length == _crackSeen)
        {
            return true;
        }

        if (screen.CrackStatus != 0)
        {
            GD.Print("Bot: code " + (screen.CrackStatus == 1 ? "cracked" : "locked out") + " in " + guesses.Length + " guesses");
            return false;
        }

        _crackSeen = guesses.Length;
        BotDriver.Type(BotDriver.NextGuess(guesses, screen.CrackExact, screen.CrackPartial));
        return true;
    }
}

/// <summary>
/// Plays an Agent Defense run: each cue's lane key as the cue crosses the line, until the
/// run is over.
/// </summary>
public sealed class DefenseStep : BotStep
{
    private int _pressed = -1;
    private bool _started;

    public DefenseStep()
        : base("play the run", 90)
    {
    }

    public override void Begin(BotBody body)
    {
        _pressed = -1;
        _started = false;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        AgentDefenseView? view = body.Me?.GetTree().GetFirstNodeInGroup(AgentDefenseView.Group) as AgentDefenseView;

        if (view == null || !view.Playing)
        {
            // Over once it had started: the server scored it.
            return _started ? StepResult.Done : StepResult.Running;
        }

        _started = true;
        int clock = view.Clock;

        foreach (DefenseCue cue in view.Cues)
        {
            if (cue.AtMs > _pressed && cue.AtMs <= clock)
            {
                Key key = AgentDefenseView.LaneKeys[cue.Lane];
                Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = true });
                Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = false });
            }
        }

        _pressed = Math.Max(_pressed, clock);
        return StepResult.Running;
    }
}

/// <summary>
/// Watches the Town cameras until a drone is in the picture and clicks it, which
/// reports it for the town's pay. The cameras change by themselves every few seconds.
/// </summary>
public sealed class CameraStep : BotStep
{
    private double _waited;

    public CameraStep()
        : base("report a drone", 45)
    {
    }

    public override void Begin(BotBody body)
    {
        _waited = 0;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        _waited += delta;
        CctvView? view = body.Me?.GetTree().GetFirstNodeInGroup(CctvView.Group) as CctvView;

        if (view == null || _waited < 1)
        {
            return StepResult.Running;
        }

        Vector2? at = view.ScreenPointOfADrone();

        if (at == null)
        {
            return StepResult.Running;
        }

        GD.Print("Bot: clicking a drone on camera " + view.CameraNumber);
        BotDriver.Click(at.Value);
        return StepResult.Done;
    }
}
