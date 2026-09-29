namespace MmoGame3d.Bots;

/// <summary>
/// A judge that watches all the time, beside whatever the bot is doing (bots.md R7):
/// stuck, out of bounds, an error in the client's log. It looks every frame and says what
/// is wrong, or nothing. What it says is a finding: recorded, and the bot plays on.
/// </summary>
public abstract class BotWatcher
{
    protected BotWatcher(string kind, bool wantsPicture)
    {
        Kind = kind;
        WantsPicture = wantsPicture;
    }

    // The finding's kind: "stuck", "client-error".
    public string Kind { get; }

    // A picture helps with what is seen (stuck, a screen), not with a log line.
    public bool WantsPicture { get; }

    // What is wrong now, or null. The same words again within a minute are not recorded
    // twice, so a lasting problem should keep its words the same.
    public abstract string? Look(BotBody body, BotStep? step, double delta);
}
