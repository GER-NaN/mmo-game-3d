namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using Godot;
using MmoGame3d.BotJudging;
using MmoGame3d.Dev.Screens;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Terminals;
using MmoGame3d.Rules.World;

/// <summary>
/// World events, as a player finds them: the phone out, Notifications read. With one
/// running, a coin flip to drop everything and go (the author's WorldEventCheck): travel
/// to its zone, walk to where it is, bring its drones down with an EMP if one is worn,
/// else stay near while they fly; then Notifications again, to see it ended and that it
/// counted. Nothing running: the phone goes away.
/// </summary>
public sealed class CheckWorldEventsActivity : StepsActivity
{
    // What a row's line tells a player: where to go. Only the swarm so far.
    private const string SwarmLine = "Drone Swarm in Meadows!";
    private const string SwarmSpot = "Events/DroneSwarm";

    private static readonly List<BotFact> NeedsList = new List<BotFact> { BotFact.PhoneAtLeast(20) };

    public CheckWorldEventsActivity()
        : base("check world events", 3)
    {
    }

    public override IReadOnlyList<BotFact> Needs
    {
        get { return NeedsList; }
    }

    public override double UsualSeconds
    {
        get { return 240; }
    }

    // Set while it runs, for the judge.
    public bool Going { get; private set; }

    public bool SawItRunning { get; private set; }

    public bool ReadAfter { get; private set; }

    public List<string> PastAfter { get; } = new List<string>();

    public string Line
    {
        get { return SwarmLine; }
    }

    public override bool CanStart(BotBody body)
    {
        return body.Me != null;
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        Going = false;
        SawItRunning = false;
        ReadAfter = false;
        PastAfter.Clear();

        List<BotStep> there = new List<BotStep>
        {
            new TravelStep(ZoneIds.Meadows),
            new WalkToStep("walk to the swarm", b => b.Thing(SwarmSpot), 6f, 120),
            new StaySwarmStep(this),
            TerminalUi.PhoneOut(),
            TerminalUi.OpenApp(TerminalApps.Notifications),
            new PauseStep(1.5),
            new DoStep("read how it ended", 5, (b, d) =>
            {
                List<string> past = TerminalUi.EventRows(b, false);

                if (past.Count == 0)
                {
                    return StepResult.Running;
                }

                PastAfter.AddRange(past);
                ReadAfter = true;
                return StepResult.Done;
            }),
        };

        return new List<BotStep>
        {
            TerminalUi.PhoneOut(),
            TerminalUi.OpenApp(TerminalApps.Notifications),
            new PauseStep(1.5),
            new DoStep("read Notifications", 5, (b, d) =>
            {
                bool running = false;

                foreach (string row in TerminalUi.EventRows(b, true))
                {
                    running = running || row.StartsWith(SwarmLine);
                }

                // Half the time a player drops everything for it; half the time not.
                Going = running && b.Random.Next(2) == 0;
                GD.Print("Bot: " + (running ? "a swarm is on, " + (Going ? "going" : "not going") : "no world event on"));
                return StepResult.Done;
            }),
            new CloseAllStep(),
            new SequenceStep("go to the world event", 420, b => Going, there),
            new CloseAllStep(),
        };
    }

    public override BotActivityJudge? NewJudge()
    {
        return new WorldEventJudge(this);
    }

    /// <summary>
    /// At the swarm while its drones fly: hunting them with an EMP worn, else standing
    /// near. Done once none has flown for a few seconds (all down, or the swarm left).
    /// </summary>
    private sealed class StaySwarmStep : BotStep
    {
        private const double Gone = 4;

        private readonly CheckWorldEventsActivity _activity;
        private HuntStep? _hunt;
        private double _noneFor;

        public StaySwarmStep(CheckWorldEventsActivity activity)
            : base("stay while the swarm flies", 220)
        {
            _activity = activity;
        }

        public override bool Walks
        {
            get { return _hunt != null; }
        }

        public override Vector3? Target(BotBody body)
        {
            return _hunt?.Target(body);
        }

        public override void Begin(BotBody body)
        {
            _hunt = null;
            _noneFor = 0;
        }

        public override StepResult Tick(BotBody body, double delta)
        {
            if (body.LiveDrones().Count == 0)
            {
                _hunt = null;
                body.Stop();
                _noneFor += delta;
                return _noneFor >= Gone ? StepResult.Done : StepResult.Running;
            }

            _noneFor = 0;
            _activity.SawItRunning = true;

            if (!body.Wears(ItemType.EmpEmitter))
            {
                return StepResult.Running;
            }

            if (_hunt == null)
            {
                _hunt = new HuntStep();
                _hunt.Begin(body);
            }

            StepResult result = _hunt.Tick(body, delta);

            if (result != StepResult.Running)
            {
                _hunt = null;
            }

            return StepResult.Running;
        }
    }
}

/// <summary>
/// A world event the bot was at, from its phone afterwards: Notifications must count it
/// (WorldEventCheck). A run that was cut short, or that never saw the swarm fly, is not
/// judged.
/// </summary>
public sealed class WorldEventJudge : BotActivityJudge
{
    private readonly CheckWorldEventsActivity _activity;

    public WorldEventJudge(CheckWorldEventsActivity activity)
    {
        _activity = activity;
    }

    public override string? After(BotBody body, BotEnd end)
    {
        if (end != BotEnd.Finished || !_activity.Going)
        {
            return null;
        }

        return WorldEventCheck.Judge(_activity.Line, _activity.SawItRunning, _activity.ReadAfter, _activity.PastAfter);
    }
}
