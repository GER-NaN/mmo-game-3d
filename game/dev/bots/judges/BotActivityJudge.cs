namespace MmoGame3d.Dev;
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
