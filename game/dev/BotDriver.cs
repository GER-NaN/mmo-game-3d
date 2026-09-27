namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Rules.Terminals;
using MmoGame3d.Ui;

/// <summary>
/// Plays a client by itself (--bot), for soak runs and headless checks. The rule, kept
/// from mmo-game: a bot may look things up, but it acts through input. It presses the
/// same keys and clicks the same buttons a person does, so everything past the keyboard
/// and mouse is the real game (BotBody).
///
/// How it plays: it picks an activity that can start where it is (BotActivities), by
/// weight, and works through that activity's steps: walk to the college, talk to the
/// registrar, work the panel, close it, emote, talk to the professor, walk out. Every
/// step has a time limit, and a step that fails ends its activity; so does a zone
/// change no step asked for (the party walked through a door). Between activities it
/// closes whatever is open, so its window shows the world. It joins any party it is
/// invited to, whatever it is doing. Back at the main menu, BotKeeper takes over.
/// </summary>
public partial class BotDriver : Node
{
    // A person takes a moment to read before clicking.
    private const double JoinAfter = 0.8;

    // Most invites are turned down, so parties stay small.
    private const double JoinChance = 0.35;

    // With nothing that can start, it looks again after this long.
    private const double PickAgain = 2;

    private readonly Random _random = new Random();
    private BotBody _body = null!;
    private BotActivity? _activity;
    private List<BotStep> _steps = new List<BotStep>();
    private int _step;
    private double _inStep;
    private string _zone = "";
    private string _lastActivity = "";
    private readonly CloseAllStep _closer = new CloseAllStep();
    private bool _closing = true;
    private double _closingFor;
    private double _pickIn;
    private double _joinSeenFor;
    private bool _joinDecided;
    private bool _willJoin;
    private BotPositionJudge _judge = null!;
    private BotZoneJudge _zoneJudge = null!;

    // Walks that failed in a row: two, and it is trapped somewhere; it escapes first.
    private int _failedWalks;

    // A goal in hand: what it wants, and the activities chosen for it one by one. A
    // quarter of goals are dropped at a random moment, wherever the bot is then.
    private const double DropChance = 0.25;
    private BotGoal? _goal;
    private GoalState _goalState = new GoalState();
    private double _goalFor;
    private double _dropAt = -1;

    // Names the bot in the judge's findings and pictures.
    public string Profile { get; set; } = "";

    public override void _Ready()
    {
        _body = new BotBody(this, _random);
        _judge = new BotPositionJudge(Profile);
        _zoneJudge = new BotZoneJudge(Profile);
        _closer.Begin(_body);
    }

    public override void _ExitTree()
    {
        _body?.Stop();
    }

    public override void _Process(double delta)
    {
        _body.Tick(delta);

        // Not in the world: loading, or the menus, which are BotKeeper's.
        if (_body.Me == null)
        {
            return;
        }

        AcceptInvites(delta);
        string doing = (_goal != null ? _goal.Name + " > " : "") + (_activity?.Name ?? (_closing ? "closing up" : "choosing"));
        _judge.Tick(_body, delta, doing, _activity != null ? _steps[_step] : null);
        _zoneJudge.Tick(_body, delta, doing);

        if (_goal != null)
        {
            _goalFor += delta;

            if (_dropAt >= 0 && _goalFor >= _dropAt)
            {
                DropGoal();
                return;
            }
        }

        if (_closing)
        {
            _closingFor += delta;

            if (_closer.Tick(_body, delta) != StepResult.Running || _closingFor > _closer.Limit)
            {
                _closing = false;
            }

            return;
        }

        if (_activity == null)
        {
            _pickIn -= delta;

            if (_pickIn <= 0 && !NextForGoal())
            {
                Pick();
            }

            return;
        }

        BotStep step = _steps[_step];

        if (_body.ZoneId != _zone && !step.MovesZone)
        {
            End("pulled from " + _zone + " to " + _body.ZoneId, false);
            return;
        }

        _inStep += delta;
        bool tooLong = _inStep > step.Limit;
        StepResult result = tooLong ? StepResult.Failed : step.Tick(_body, delta);

        switch (result)
        {
            case StepResult.Running:
                break;
            case StepResult.Failed:
                _failedWalks = step.Walks ? _failedWalks + 1 : 0;

                if (step.Walks && !tooLong)
                {
                    _judge.WalkFailed(_body, (_goal != null ? _goal.Name + " > " : "") + (_activity?.Name ?? ""), step);
                }

                End("\"" + step.Name + "\" " + (tooLong ? "took too long" : "failed"), false);
                break;
            default:
                _zone = _body.ZoneId;
                _step++;

                if (_step >= _steps.Count)
                {
                    _failedWalks = 0;
                    End("done", true);
                }
                else
                {
                    StartStep();
                }

                break;
        }
    }

    // By weight among those that can start here, and not the same one twice running
    // when there is a choice.
    private void Pick()
    {
        if (_failedWalks >= 2)
        {
            _failedWalks = 0;
            Start(BotActivities.Escape);
            return;
        }

        List<BotActivity> open = new List<BotActivity>();
        List<BotGoal> goals = new List<BotGoal>();
        int total = 0;

        foreach (BotActivity activity in BotActivities.All)
        {
            if (activity.CanStart(_body))
            {
                open.Add(activity);
                total += activity.Weight;
            }
        }

        foreach (BotGoal goal in BotGoals.All)
        {
            if (goal.CanStart(_body))
            {
                goals.Add(goal);
                total += goal.Weight;
            }
        }

        // A goal, by the same weights as the activities.
        int goalRoll = _random.Next(Math.Max(1, total));

        foreach (BotGoal goal in goals)
        {
            if (goalRoll < goal.Weight)
            {
                StartGoal(goal);
                return;
            }

            goalRoll -= goal.Weight;
        }

        total = 0;

        foreach (BotActivity activity in open)
        {
            total += activity.Weight;
        }

        if (open.Count > 1 && open.Exists(a => a.Name == _lastActivity))
        {
            BotActivity last = open.Find(a => a.Name == _lastActivity)!;
            open.Remove(last);
            total -= last.Weight;
        }

        if (open.Count == 0)
        {
            _pickIn = PickAgain;
            return;
        }

        GD.Print("Bot: wander mode");

        int roll = _random.Next(total);
        BotActivity pick = open[open.Count - 1];

        foreach (BotActivity activity in open)
        {
            if (roll < activity.Weight)
            {
                pick = activity;
                break;
            }

            roll -= activity.Weight;
        }

        Start(pick);
    }

    private void Start(BotActivity activity)
    {
        _activity = activity;
        _lastActivity = activity.Name;
        _steps = activity.Plan(_body);
        _step = 0;
        _zone = _body.ZoneId;
        GD.Print("Bot: starting \"" + activity.Name + "\"");
        StartStep();
    }

    private void StartGoal(BotGoal goal)
    {
        _goal = goal;
        _goalState = new GoalState();
        _goalFor = 0;
        _dropAt = _random.NextDouble() < DropChance ? 3 + (_random.NextDouble() * (goal.UsualSeconds - 3)) : -1;
        GD.Print("Bot: goal \"" + goal.Name + "\"" + (_dropAt >= 0 ? " (will drop it after " + (int)_dropAt + " s)" : ""));

        if (!NextForGoal())
        {
            Pick();
        }
    }

    // The goal's next activity, started; false with no goal, or when it is met or out of
    // activities.
    private bool NextForGoal()
    {
        if (_goal == null)
        {
            return false;
        }

        if (_goalState.Rounds >= _goal.Budget)
        {
            GD.Print("Bot: gave up goal \"" + _goal.Name + "\": out of activities");
            _goal = null;
            return false;
        }

        // Its own business: out of any party first.
        if (_goal.Solo && BotActivities.LeaveParty.CanStart(_body))
        {
            Start(BotActivities.LeaveParty);
            return true;
        }

        BotActivity? next = _goal.Next(_body, _goalState);

        if (next == null)
        {
            GD.Print(_goalState.GiveUp.Length > 0 ? "Bot: gave up goal \"" + _goal.Name + "\": " + _goalState.GiveUp : "Bot: goal met \"" + _goal.Name + "\"");
            _goal = null;
            return false;
        }

        // Somewhere else than the next thing needs (pulled away, or a plan cut short):
        // back to town first.
        if (!next.CanStart(_body) && BotActivities.GoBackToTown.CanStart(_body))
        {
            Start(BotActivities.GoBackToTown);
            return true;
        }

        if (!next.CanStart(_body))
        {
            GD.Print("Bot: gave up goal \"" + _goal.Name + "\": cannot " + next.Name + " here");
            _goal = null;
            return false;
        }

        Start(next);
        return true;
    }

    // Walks off from whatever it was doing, mid-step, leaving it as it is: the state a
    // distracted player leaves behind.
    private void DropGoal()
    {
        GD.Print("Bot: dropped goal \"" + _goal?.Name + "\" after " + (int)_goalFor + " s, during \"" + (_activity?.Name ?? "") + "\""
            + (_activity != null ? " at \"" + _steps[_step].Name + "\"" : ""));
        _goal = null;
        _activity = null;
        _closing = false;
        _body.Stop();
        _pickIn = 0;
    }

    private void StartStep()
    {
        _inStep = 0;
        _steps[_step].Begin(_body);
    }

    private void End(string how, bool finished)
    {
        if (_goal != null)
        {
            _goalState.Rounds++;
            _goalState.LastActivity = _activity?.Name ?? "";
            _goalState.LastFinished = finished;
        }

        GD.Print("Bot: " + (finished ? "finished" : "gave up on") + " \"" + _activity?.Name + "\": " + how);
        _activity = null;
        _body.Stop();
        _closing = true;
        _closingFor = 0;
        _closer.Begin(_body);
    }

    private void AcceptInvites(double delta)
    {
        Button? join = _body.Usable(InvitePrompt.JoinGroup);

        if (join == null)
        {
            _joinSeenFor = 0;
            _joinDecided = false;
            return;
        }

        if (!_joinDecided)
        {
            // On a solo goal, never; in wander mode, now and then.
            _joinDecided = true;
            _willJoin = (_goal == null || !_goal.Solo) && _random.NextDouble() < JoinChance;
        }

        _joinSeenFor += delta;

        if (_joinSeenFor >= JoinAfter)
        {
            _joinSeenFor = 0;
            Button? no = _body.Usable(InvitePrompt.NoGroup);
            Button? answer = _willJoin || no == null ? join : no;
            GD.Print("Bot: clicking " + answer.Text);
            _body.Click(answer);
        }
    }

    // A real press and release at a screen point, through the same input queue a mouse
    // feeds, so the GUI and the picker both see it.
    public static void Click(Vector2 at)
    {
        InputEventMouseButton press = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = at, GlobalPosition = at };
        InputEventMouseButton release = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = at, GlobalPosition = at };
        Input.ParseInputEvent(press);
        Input.ParseInputEvent(release);
    }

    // Key by key into whatever has the focus, then Enter.
    public static void Type(string text)
    {
        foreach (char c in text)
        {
            Key key = KeyFor(c);
            Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Unicode = c, Pressed = true });
            Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Unicode = c, Pressed = false });
        }

        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Enter, PhysicalKeycode = Key.Enter, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Enter, PhysicalKeycode = Key.Enter, Pressed = false });
    }

    private static Key KeyFor(char c)
    {
        if (char.IsDigit(c))
        {
            return Key.Key0 + (c - '0');
        }

        if (char.IsLetter(c))
        {
            return Key.A + (char.ToUpperInvariant(c) - 'A');
        }

        switch (c)
        {
            case ' ':
                return Key.Space;
            case '/':
                return Key.Slash;
            case '.':
                return Key.Period;
            case ',':
                return Key.Comma;
            case '?':
                return Key.Question;
            case '!':
                return Key.Exclam;
            case '-':
                return Key.Minus;
            default:
                return Key.Unknown;
        }
    }

    // The first code, in order, that would have given every answer seen so far.
    public static string NextGuess(string[] guesses, int[] exact, int[] partial)
    {
        int count = 1;

        for (int i = 0; i < CodeCracker.Length; i++)
        {
            count *= CodeCracker.Digits;
        }

        for (int n = 0; n < count; n++)
        {
            char[] digits = new char[CodeCracker.Length];
            int rest = n;

            for (int i = CodeCracker.Length - 1; i >= 0; i--)
            {
                digits[i] = (char)('0' + (rest % CodeCracker.Digits));
                rest /= CodeCracker.Digits;
            }

            string candidate = new string(digits);
            bool fits = true;

            for (int g = 0; g < guesses.Length && fits; g++)
            {
                CodeCracker check = new CodeCracker(candidate);
                check.Guess(guesses[g]);
                fits = check.Exact[0] == exact[g] && check.Partial[0] == partial[g];
            }

            if (fits)
            {
                return candidate;
            }
        }

        return "0000";
    }
}
