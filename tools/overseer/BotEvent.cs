namespace MmoGame3d.Overseer;

/// <summary>
/// One line of a bot's events file (game/bots/BotEventLog.cs writes them).
/// </summary>
public class BotEvent
{
    public BotEvent(string time, string kind, string detail)
    {
        Time = time;
        Kind = kind;
        Detail = detail;
    }

    public string Time { get; }

    public string Kind { get; }

    public string Detail { get; }
}
