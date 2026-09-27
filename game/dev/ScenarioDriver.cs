namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Networking;
using MmoGame3d.Rules.Terminals;
using MmoGame3d.Ui;

/// <summary>
/// Runs one dev test scenario (--scenario name) against a server started with
/// --dev-scenarios, which has already set the player up for it (ServerScenarios). It acts
/// through input like the bot, one step after another, prints "SCENARIO name: PASS" or
/// "FAIL" with the reason, and quits with exit code 0 or 1. Every scenario ends within
/// Limit seconds.
/// </summary>
public partial class ScenarioDriver : Node
{
    private const double Limit = 50;
    private const double StepPause = 0.4;

    private readonly string _name;
    private readonly Networks _networks;
    private readonly List<string> _notices = new List<string>();
    private readonly List<Func<bool>> _steps = new List<Func<bool>>();
    private readonly List<string> _stepNames = new List<string>();
    private int _step;
    private double _elapsed;
    private double _pause = 2;
    private int _defensePressed = -1;
    private bool _done;
    private bool _pageSeen;
    private bool _plantMade;
    private bool _bought;
    private string _zone = "";
    private double _zoneSince;
    private float _lowest = float.MaxValue;
    private string _lastStatus = "";
    private int _guessesSeen = -1;
    private double _releaseIn = -1;
    private double _walkedFor;
    private float _lastY = float.NaN;
    private double _sinceRiseSample;
    private float _fastestRise;
    private bool _sawAirborne;
    private float _headingBefore;
    private float _distanceBefore;
    private double _turnHeld = -1;

    public ScenarioDriver(string name, Networks networks)
    {
        _name = name;
        _networks = networks;
    }

    public override void _Ready()
    {
        _networks.Session.NoticeReceived += text => _notices.Add(text);
        _networks.Subway.PageReceived += (page, pages, lines) => _pageSeen = true;
        _networks.Session.ZoneChanged += zone =>
        {
            _zone = zone;
            _zoneSince = _elapsed;
        };
        _networks.Session.IntentAnswered += (id, refusal) => _bought = _bought || refusal.Length == 0;
        _networks.Garden.PlantMade += (id, name, reward) => _plantMade = true;
        _networks.Terminal.StatusReceived += lines => _lastStatus = lines.Length > 0 ? lines[0] : "";
        GD.Print("SCENARIO " + _name + ": started");

        switch (_name)
        {
            case "cracker":
                GoOnline();
                CrackACode();
                Expect("a code cracked", () => Noticed("Code cracked."));
                Expect("the achievement", () => Noticed("Achievement: Code cracker"));
                break;
            case "rootkit":
                GoOnline();
                CrackACode();
                Expect("the taxis cleaned", () => _lastStatus.Contains("cleaned the rootkit"));
                break;
            case "defense":
                GoOnline();
                Step("open Defense Objectives", () => ClickGroup(TerminalScreen.AppGroupPrefix + TerminalApps.Defense));
                Step("start a run", () => ClickGroup(TerminalScreen.DefenseStartGroup));
                Step("play the run", PlayDefense);
                Expect("every cue hit", () => Noticed("Agent Defense:") && Noticed(" 0 missed"));
                break;
            case "cameras":
                GoOnline();
                Step("open Town cameras", () => ClickGroup(TerminalScreen.AppGroupPrefix + TerminalApps.TownCameras));
                Step("click a drone", ClickDrone);
                Expect("the drone reported", () => Noticed("drone reported"));
                Step("hold a turn key", () => HoldTurn(0.6));
                Expect("the body not turned behind the screen", () => Mathf.Abs(Mathf.AngleDifference(_headingBefore, MyHeading())) < 0.01f);
                break;
            case "subway":
                Use("Spray your name");
                Expect("the tag", () => Noticed("on the wall"));
                break;
            case "book":
                Use("visitor book");
                Expect("a page of the book", () => _pageSeen);
                break;
            case "college":
                Step("walk in at the door", () =>
                {
                    Input.ActionPress("move_forward");
                    return _zone == "college";
                });
                Step("stop", () =>
                {
                    Input.ActionRelease("move_forward");
                    return true;
                });
                break;
            case "meadows":
                Step("walk in at the door", () =>
                {
                    Input.ActionPress("move_forward");
                    return _zone == "meadows";
                });
                Step("stop", () =>
                {
                    Input.ActionRelease("move_forward");
                    return true;
                });
                Expect("standing on the terrain", StandingInMeadows);
                break;
            case "hills":
                Step("land", () =>
                {
                    Players.Player? me = GetTree().GetFirstNodeInGroup(Players.Player.LocalGroup) as Players.Player;
                    return me != null && me.IsOnFloor() && _elapsed > 1.5;
                });
                Step("walk up the hill", () =>
                {
                    Input.ActionPress("move_forward");
                    return _walkedFor > 2.5;
                });
                Step("stop", () =>
                {
                    Input.ActionRelease("move_forward");
                    return true;
                });
                Expect("a climb faster than the old jump guess", () => _fastestRise > 1.2f);
                Expect("never airborne on the way", () => !_sawAirborne);
                break;
            case "lights":
                Use("junction box");
                Expect("the lights on", () => Noticed("Achievement: Lights on"));
                break;
            case "taxi":
                Use("Call a robo taxi");
                Expect("the ride", () => _zone.StartsWith("taxi"));
                Expect("sitting inside the cabin, not on its roof", InsideTheCabin);
                break;
            case "fix":
                Use("Fix the traffic light");
                Expect("it fixed", () => Noticed("You fixed the traffic light."));
                break;
            case "shop":
                Step("wheel over the world", () => WheelAt(GetViewport().GetVisibleRect().Size * new Vector2(0.5f, 0.3f)));
                Expect("the camera zoomed", () => CameraDistance() != _distanceBefore);
                Use("Talk to Dee");
                Step("buy the first thing", () => ClickGroup(ShopPanel.BuyGroup));
                Expect("it bought", () => _bought);
                Step("wheel over the shop", () => WheelOverPanel<ShopPanel>(ShopPanel.BuyGroup));
                Expect("the camera not zoomed", () => CameraDistance() == _distanceBefore);
                break;
            case "registrar":
                Use("Talk to Mara");
                Step("wheel over the registrar's panel", () => WheelOverPanel<CollegePanel>(CollegePanel.ClassGroup));
                Expect("the camera not zoomed", () => CameraDistance() == _distanceBefore);
                break;
            case "garden":
                Use("Make a house plant");
                Step("plant a piece", () => ClickGroup(Gardening.GardenScreen.PieceGroup));
                Step("complete", () => ClickGroup(Gardening.GardenScreen.CompleteGroup));
                Step("finish", () => ClickGroup(Gardening.GardenScreen.FinishGroup));
                Expect("the plant made", () => _plantMade);
                break;
            case "workbench":
                Use("workbench");
                Step("take the battery out", () => ClickGroup(WorkbenchPanel.RemoveGroup));
                Step("put a new battery in", () => ClickGroup(WorkbenchPanel.InsertGroup));
                Expect("the phone at 100%", () => LabelShows("battery 100%"));
                break;
            default:
                Finish(false, "no such scenario");
                return;
        }
    }

    public override void _Process(double delta)
    {
        if (_done)
        {
            return;
        }

        _elapsed += delta;

        // The lowest the player has been since arriving in the meadows: a fall through the
        // ground and a snap back up would pass a check made only at the end.
        Players.Player? me = GetTree().GetFirstNodeInGroup(Players.Player.LocalGroup) as Players.Player;

        if (_zone == "meadows" && me != null)
        {
            _lowest = Mathf.Min(_lowest, me.GlobalPosition.Y);
        }

        // A walk uphill: how fast it climbed, and whether the server ever called it a jump.
        if (_name == "hills" && me != null && Input.IsActionPressed("move_forward"))
        {
            _walkedFor += delta;
            _sinceRiseSample += delta;

            // Over a quarter second: a single frame's rise jumps when the client corrects.
            if (float.IsNaN(_lastY))
            {
                _lastY = me.GlobalPosition.Y;
                _sinceRiseSample = 0;
            }
            else if (_sinceRiseSample >= 0.25)
            {
                _fastestRise = Mathf.Max(_fastestRise, (me.GlobalPosition.Y - _lastY) / (float)_sinceRiseSample);
                _lastY = me.GlobalPosition.Y;
                _sinceRiseSample = 0;
            }

            _sawAirborne = _sawAirborne || me.IsAirborne;
        }

        if (_releaseIn >= 0)
        {
            _releaseIn -= delta;

            if (_releaseIn < 0)
            {
                Input.ActionRelease("interact");
            }
        }

        if (_elapsed > Limit)
        {
            Finish(false, "timed out at step \"" + _stepNames[Math.Min(_step, _stepNames.Count - 1)] + "\"; notices: " + string.Join(" | ", _notices));
            return;
        }

        _pause -= delta;

        if (_pause > 0)
        {
            return;
        }

        if (_step >= _steps.Count)
        {
            Finish(true, "");
            return;
        }

        if (_steps[_step]())
        {
            GD.Print("SCENARIO " + _name + ": done \"" + _stepNames[_step] + "\"");
            _step++;
            _pause = StepPause;
        }
    }

    private void Step(string name, Func<bool> step)
    {
        _stepNames.Add(name);
        _steps.Add(step);
    }

    private void Expect(string name, Func<bool> check)
    {
        Step("expect " + name, check);
    }

    // Holds turn_left this long, noting the heading before; true once released. With no
    // control focused, as after clicking the picture: a focused button takes the keys.
    private bool HoldTurn(double seconds)
    {
        if (_turnHeld < 0)
        {
            GetViewport().GuiReleaseFocus();
            _headingBefore = MyHeading();
            _turnHeld = 0;
            Input.ActionPress("turn_left");
            return false;
        }

        _turnHeld += GetProcessDeltaTime();

        if (_turnHeld < seconds)
        {
            return false;
        }

        Input.ActionRelease("turn_left");
        return true;
    }

    // The pointer moves there first, as a real one does: the GUI knows what it is over
    // from the motion.
    private bool WheelAt(Vector2 at)
    {
        _distanceBefore = CameraDistance();
        Input.ParseInputEvent(new InputEventMouseMotion { Position = at, GlobalPosition = at });
        Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = MouseButton.WheelUp, Pressed = true, Position = at, GlobalPosition = at });
        Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = MouseButton.WheelUp, Pressed = false, Position = at, GlobalPosition = at });
        return true;
    }

    // Over the middle of the panel holding a button of this group.
    private bool WheelOverPanel<T>(string buttonGroup)
        where T : Control
    {
        Node? node = GetTree().GetFirstNodeInGroup(buttonGroup);

        while (node != null && !(node is T))
        {
            node = node.GetParent();
        }

        Control? panel = node as Control;
        return panel != null && WheelAt(panel.GetGlobalRect().GetCenter());
    }

    private float CameraDistance()
    {
        Players.ChaseCamera? camera = GetViewport().GetCamera3D() as Players.ChaseCamera;
        return camera != null ? camera.Distance : 0f;
    }

    private float MyHeading()
    {
        Players.Player? me = GetTree().GetFirstNodeInGroup(Players.Player.LocalGroup) as Players.Player;
        return me != null ? me.Heading : 0f;
    }

    private void GoOnline()
    {
        Use("Go Online");
    }

    // F at the thing whose prompt shows this text.
    private void Use(string prompt)
    {
        Step("use \"" + prompt + "\"", () =>
        {
            Label? label = GetTree().GetFirstNodeInGroup(Hud.PromptGroup) as Label;

            if (label == null || !label.Visible || !label.Text.Contains(prompt))
            {
                return false;
            }

            // Held a moment, as a key is: the game looks for it down, then up.
            Input.ActionPress("interact");
            _releaseIn = 0.15;
            return true;
        });
    }

    private void CrackACode()
    {
        Step("open the code cracker", () => ClickGroup(TerminalScreen.AppGroupPrefix + TerminalApps.CodeCracker));
        Step("start a code", () => ClickGroup(TerminalScreen.CrackStartGroup));
        Step("guess until cracked", () =>
        {
            TerminalScreen? screen = GetTree().GetFirstNodeInGroup(TerminalScreen.GoOfflineGroup)?.Owner as TerminalScreen;
            string[]? guesses = screen?.CrackGuesses;

            if (screen == null || guesses == null)
            {
                return false;
            }

            if (screen.CrackStatus != 0)
            {
                return true;
            }

            if (guesses.Length != _guessesSeen)
            {
                _guessesSeen = guesses.Length;
                BotDriver.Type(BotDriver.NextGuess(guesses, screen.CrackExact, screen.CrackPartial));
            }

            return false;
        });
    }

    // Reads the chart as a person reads the screen, and presses each lane's key as its
    // cue reaches the line.
    private bool PlayDefense()
    {
        // Over when the server has scored it (the app is rebuilt with the board then).
        if (Noticed("Agent Defense:"))
        {
            return true;
        }

        AgentDefenseView? view = GetTree().GetFirstNodeInGroup(AgentDefenseView.Group) as AgentDefenseView;

        if (view == null || !view.Playing)
        {
            return false;
        }

        int clock = view.Clock;

        foreach (DefenseCue cue in view.Cues)
        {
            if (cue.AtMs > _defensePressed && cue.AtMs <= clock)
            {
                Key key = AgentDefenseView.LaneKeys[cue.Lane];
                Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = true });
                Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = false });
            }
        }

        _defensePressed = Math.Max(_defensePressed, clock);
        return false;
    }

    // On the ground, not falling through it: the terrain's collision works on both sides
    // (the server corrects a client that it sees falling).
    private bool StandingInMeadows()
    {
        Players.Player? me = GetTree().GetFirstNodeInGroup(Players.Player.LocalGroup) as Players.Player;
        return _zone == "meadows" && me != null && me.IsOnFloor() && _lowest > -0.5f && _elapsed > _zoneSince + 2.0;
    }

    // A moment in, on the cabin's floor: the rider's feet, zone-local, near its height.
    private bool InsideTheCabin()
    {
        Players.Player? me = GetTree().GetFirstNodeInGroup(Players.Player.LocalGroup) as Players.Player;
        return me != null && _elapsed > _zoneSince + 1.5 && me.IsOnFloor() && me.Position.Y < 0.8f;
    }

    private bool ClickDrone()
    {
        CctvView? view = GetTree().GetFirstNodeInGroup(CctvView.Group) as CctvView;
        Vector2? at = view?.ScreenPointOfADrone();

        // None in this picture: on to the next camera, as a person would.
        if (at == null)
        {
            view?.Show(view.CameraNumber);
            return false;
        }

        BotDriver.Click(at.Value);
        return true;
    }

    private bool ClickGroup(string group)
    {
        Button? button = GetTree().GetFirstNodeInGroup(group) as Button;

        if (button == null || !button.IsVisibleInTree() || button.Disabled)
        {
            return false;
        }

        BotDriver.Click(button.GetGlobalRect().GetCenter());
        return true;
    }

    private bool Noticed(string text)
    {
        foreach (string notice in _notices)
        {
            if (notice.Contains(text))
            {
                return true;
            }
        }

        return false;
    }

    private bool LabelShows(string text)
    {
        foreach (Node node in GetTree().Root.FindChildren("*", "Label", true, false))
        {
            Label? label = node as Label;

            if (label != null && label.IsVisibleInTree() && label.Text.Contains(text))
            {
                return true;
            }
        }

        return false;
    }

    private void Finish(bool passed, string why)
    {
        _done = true;
        GD.Print("SCENARIO " + _name + ": " + (passed ? "PASS" : "FAIL " + why) + " in " + _elapsed.ToString("0.0") + " s");

        // With --screenshot as well, the picture is the point: it quits once taken.
        if (GetTree().Root.FindChild("Screenshot", true, false) != null)
        {
            return;
        }

        GetTree().Quit(passed ? 0 : 1);
    }
}
