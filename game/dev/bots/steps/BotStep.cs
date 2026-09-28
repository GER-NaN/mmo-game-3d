namespace MmoGame3d.Dev;
using Godot;

/// <summary>
/// One thing a bot does inside an activity: walk somewhere, use something, click a
/// button, wait. Each says when it is done, and each has a time limit; past it the step
/// has failed, and so has its activity. That is every activity's way out.
/// </summary>
public abstract class BotStep
{
    protected BotStep(string name, double limit)
    {
        Name = name;
        Limit = limit;
    }

    public string Name { get; }

    public double Limit { get; }

    // Steps that take the bot to another zone; any other zone change is a surprise.
    public virtual bool MovesZone
    {
        get { return false; }
    }

    // Where the step is taking the bot, if anywhere: for the judge's records.
    public virtual Vector3? Target(BotBody body)
    {
        return null;
    }

    // A step that walks: the judge expects the bot to move during it.
    public virtual bool Walks
    {
        get { return false; }
    }

    // A step that pushes against what stops a player, on purpose (the escaper at the
    // edge, the wedger in a gap): not stuck, not thrashing, while it lasts.
    public virtual bool Presses
    {
        get { return false; }
    }

    public virtual void Begin(BotBody body)
    {
    }

    public abstract StepResult Tick(BotBody body, double delta);
}
