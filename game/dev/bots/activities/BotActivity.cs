namespace MmoGame3d.Dev;

using System.Collections.Generic;

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
