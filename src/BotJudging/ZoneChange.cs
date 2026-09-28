namespace MmoGame3d.BotJudging;
/// <summary>A zone change: when, from where, to where.</summary>
public sealed class ZoneChange
{
    public ZoneChange(double time, string from, string to, bool planned)
    {
        Time = time;
        From = from;
        To = to;
        Planned = planned;
    }

    public double Time { get; }

    public string From { get; }

    public string To { get; }

    // Asked for by a step that goes through doors (a route), not a surprise.
    public bool Planned { get; }
}
