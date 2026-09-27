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
    private string _lastStatus = "";
    private int _guessesSeen = -1;
    private double _releaseIn = -1;

    public ScenarioDriver(string name, Networks networks)
    {
        _name = name;
        _networks = networks;
    }

    public override void _Ready()
    {
        _networks.Session.NoticeReceived += text => _notices.Add(text);
        _networks.Subway.PageReceived += (page, pages, lines) => _pageSeen = true;
        _networks.Session.ZoneChanged += zone => _zone = zone;
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
            case "lights":
                Use("junction box");
                Expect("the lights on", () => Noticed("Achievement: Lights on"));
                break;
            case "taxi":
                Use("Call a robo taxi");
                Expect("the ride", () => _zone.StartsWith("taxi"));
                break;
            case "fix":
                Use("Fix the traffic light");
                Expect("it fixed", () => Noticed("You fixed the traffic light."));
                break;
            case "shop":
                Use("Talk to Dee");
                Step("buy the first thing", () => ClickGroup(ShopPanel.BuyGroup));
                Expect("it bought", () => _bought);
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
