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

    public override void _Ready()
    {
        _body = new BotBody(this, _random);
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

            if (_pickIn <= 0)
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
                End("\"" + step.Name + "\" " + (tooLong ? "took too long" : "failed"), false);
                break;
            default:
                _zone = _body.ZoneId;
                _step++;

                if (_step >= _steps.Count)
                {
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
        List<BotActivity> open = new List<BotActivity>();
        int total = 0;

        foreach (BotActivity activity in BotActivities.All)
        {
            if (activity.CanStart(_body))
            {
                open.Add(activity);
                total += activity.Weight;
            }
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

        _activity = pick;
        _lastActivity = pick.Name;
        _steps = pick.Plan(_body);
        _step = 0;
        _zone = _body.ZoneId;
        GD.Print("Bot: starting \"" + pick.Name + "\"");
        StartStep();
    }

    private void StartStep()
    {
        _inStep = 0;
        _steps[_step].Begin(_body);
    }

    private void End(string how, bool finished)
    {
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
            return;
        }

        _joinSeenFor += delta;

        if (_joinSeenFor >= JoinAfter)
        {
            _joinSeenFor = 0;
            GD.Print("Bot: clicking Join");
            _body.Click(join);
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
