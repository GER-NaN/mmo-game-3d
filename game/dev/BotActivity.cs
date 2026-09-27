namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;

// How an activity is picked: chosen by weight when the bot is free, or an aside, which
// also fires on a timer and may pause whatever runs (BotDriver).
public enum BotTiming
{
    Chosen,
    Aside,
}

// How a run ended, for its judge and its chain.
public enum BotEnd
{
    Finished,
    Failed,
    Cancelled,
}

/// <summary>
/// Stopped mid-way, by the interrupt roller or a lost connection: let go of held keys and
/// leave every screen as it is, the state a distracted player leaves. Tidying up is the
/// normal end of an activity, or the next one's business, never a cancel's.
/// </summary>
public interface ICancellable
{
    void Cancel(BotBody body, string reason);
}

/// <summary>
/// One thing a bot does, start to end: make a plant, emote, buy a battery. It says where it
/// happens (Zone: the driver routes there first, BotRouter), what it needs and what it
/// gives (facts, for goals: BotResolver), how it is picked (Timing), and it runs frame by
/// frame until done, failed or cancelled. Its judge watches it (NewJudge).
/// </summary>
public abstract class BotActivity : ICancellable
{
    private static readonly List<BotFact> None = new List<BotFact>();

    protected BotActivity(string name, int weight)
    {
        Name = name;
        Weight = weight;
    }

    public string Name { get; }

    // How often it is chosen, against the others that can start; 0 for one only goals and
    // chains start.
    public int Weight { get; }

    // The zone it happens in, or "" for wherever the bot is.
    public virtual string Zone
    {
        get { return ""; }
    }

    public virtual BotTiming Timing
    {
        get { return BotTiming.Chosen; }
    }

    // An aside's mean seconds between firings while the bot is free; three times as long
    // during another activity.
    public virtual double AsideEvery
    {
        get { return 60; }
    }

    // Whether an aside may pause it. Most may: an emote mid-walk is worth testing.
    public virtual bool AllowsAsides
    {
        get { return true; }
    }

    // About how long it takes: a random cancel falls somewhere inside it.
    public virtual double UsualSeconds
    {
        get { return 45; }
    }

    // What must hold before it starts, besides its zone.
    public virtual IReadOnlyList<BotFact> Needs
    {
        get { return None; }
    }

    // What holds after it finishes. Unkept, it is a finding.
    public virtual IReadOnlyList<BotFact> Gives
    {
        get { return None; }
    }

    // The step running now, for the judges and the logs; null between steps.
    public virtual BotStep? Step
    {
        get { return null; }
    }

    // Why it failed, for the log; "" while it runs or when it finished.
    public string Why { get; protected set; } = "";

    // Failed on a walk to a thing that is there: two in a row and the bot is trapped.
    public bool FailedWalking { get; protected set; }

    // Beyond its zone and needs: whether it can start now (a drone flying, a free table).
    public virtual bool CanStart(BotBody body)
    {
        return true;
    }

    // Its needs hold (the zone aside, which the router sees to).
    public bool NeedsMet(BotBody body)
    {
        foreach (BotFact need in Needs)
        {
            if (!need.IsTrue(body))
            {
                return false;
            }
        }

        return true;
    }

    public abstract void Begin(BotBody body);

    public abstract StepResult Tick(BotBody body, double delta);

    public virtual void Cancel(BotBody body, string reason)
    {
        body.Stop();
    }

    // A fresh judge for one run, or null when the general judges are enough.
    public virtual BotActivityJudge? NewJudge()
    {
        return null;
    }
}

/// <summary>
/// Watches one run of an activity, as a player would: only what the client shows (where
/// the body is, what is open, what the prompt says, the bag and the money). Before looks
/// at the context; Watch may fail the run while it goes; After judges how it ended. A
/// reason returned is a finding ("activity-failed").
/// </summary>
public abstract class BotActivityJudge
{
    public virtual void Before(BotBody body)
    {
    }

    public virtual string? Watch(BotBody body, double delta)
    {
        return null;
    }

    public virtual string? After(BotBody body, BotEnd end)
    {
        return null;
    }
}

/// <summary>
/// An activity as a list of steps, run in turn; each has a time limit, and a step that
/// fails ends it, as does a zone change no step asked for (a party pull). Most activities
/// are one: give Plan the steps, or override Plan.
/// </summary>
public class StepsActivity : BotActivity
{
    private readonly Func<BotBody, bool>? _canStart;
    private readonly Func<BotBody, List<BotStep>>? _plan;
    private readonly string _zone = "";
    private List<BotStep> _steps = new List<BotStep>();
    private int _step;
    private double _inStep;
    private string _zoneNow = "";

    public StepsActivity(string name, int weight, Func<BotBody, bool> canStart, Func<BotBody, List<BotStep>> plan)
        : base(name, weight)
    {
        _canStart = canStart;
        _plan = plan;
    }

    // One that starts in a zone: the router takes the bot there first, from anywhere.
    public StepsActivity(string name, int weight, string zone, Func<BotBody, List<BotStep>> plan)
        : base(name, weight)
    {
        _zone = zone;
        _plan = plan;
    }

    protected StepsActivity(string name, int weight)
        : base(name, weight)
    {
    }

    public override string Zone
    {
        get { return _zone; }
    }

    public override BotStep? Step
    {
        get { return _step < _steps.Count ? _steps[_step] : null; }
    }

    public override bool CanStart(BotBody body)
    {
        return _canStart == null || _canStart(body);
    }

    protected virtual List<BotStep> Plan(BotBody body)
    {
        return _plan != null ? _plan(body) : new List<BotStep>();
    }

    public override void Begin(BotBody body)
    {
        _steps = Plan(body);
        _step = 0;
        _zoneNow = body.ZoneId;
        Why = "";
        FailedWalking = false;
        StartStep(body);
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        if (_step >= _steps.Count)
        {
            return StepResult.Done;
        }

        BotStep step = _steps[_step];

        if (body.ZoneId != _zoneNow && !step.MovesZone)
        {
            Why = "pulled from " + _zoneNow + " to " + body.ZoneId;
            return StepResult.Failed;
        }

        _inStep += delta;
        bool tooLong = _inStep > step.Limit;
        StepResult result = tooLong ? StepResult.Failed : step.Tick(body, delta);

        switch (result)
        {
            case StepResult.Running:
                return StepResult.Running;
            case StepResult.Failed:
                // A walk failed at a thing that is there; a missing one is not a walk.
                FailedWalking = step.Walks && step.Target(body) != null && !tooLong;

                if (FailedWalking)
                {
                    body.WalkFailed?.Invoke(step);
                }

                // A step may have said why already (a refusal it saw).
                if (Why.Length == 0)
                {
                    Why = "\"" + step.Name + "\" " + (tooLong ? "took too long" : "failed");
                }

                return StepResult.Failed;
            default:
                _zoneNow = body.ZoneId;
                _step++;

                if (_step >= _steps.Count)
                {
                    return StepResult.Done;
                }

                StartStep(body);
                return StepResult.Running;
        }
    }

    private void StartStep(BotBody body)
    {
        _inStep = 0;

        if (_step < _steps.Count)
        {
            _steps[_step].Begin(body);
        }
    }
}
