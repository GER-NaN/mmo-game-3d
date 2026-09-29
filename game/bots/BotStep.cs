namespace MmoGame3d.Bots;

/// <summary>
/// One thing a bot does, over as many frames as it takes: wait for a screen, click, walk
/// to a zone. The activity's run starts it, ticks it every frame until it is done or
/// failed, and fails it when it runs past its time limit.
/// </summary>
public abstract class BotStep
{
    public const double DefaultTimeLimit = 10;

    protected BotStep(string name, double timeLimit)
    {
        Name = name;
        TimeLimit = timeLimit;
    }

    // What the events file calls it: "click Quit", "enter meadows".
    public string Name { get; }

    public double TimeLimit { get; }

    public string FailReason { get; private set; } = "";

    // What it does with the body, for the watchers and the chatter.
    public virtual BotIntent Intent
    {
        get { return BotIntent.Idle; }
    }

    public virtual void Start(BotBody body)
    {
    }

    public abstract BotStepState Tick(BotBody body, double delta);

    // After the step ends, however it ends: let go of what it holds.
    public virtual void End(BotBody body)
    {
    }

    protected BotStepState Fail(string reason)
    {
        FailReason = reason;
        return BotStepState.Failed;
    }
}
