namespace MmoGame3d.Overseer;

/// <summary>
/// One line of a bot's events file (game/bots/BotEventLog.cs writes them).
/// </summary>
public class BotEvent
{
    public BotEvent(string time, string kind, string detail, string activity)
    {
        Time = time;
        Kind = kind;
        Detail = detail;
        Activity = activity;
    }

    public string Time { get; }

    public string Kind { get; }

    public string Detail { get; }

    // The activity it happened in; empty outside one.
    public string Activity { get; }
}
