namespace MmoGame3d.BotJudging;

using System.Collections.Generic;

/// <summary>A zone change: when, from where, to where.</summary>
public sealed class ZoneChange
{
    public ZoneChange(double time, string from, string to)
    {
        Time = time;
        From = from;
        To = to;
    }

    public double Time { get; }

    public string From { get; }

    public string To { get; }
}

/// <summary>
/// How a bot moves between zones, from its changes in the last Window seconds:
///
/// - Churn: more than MaxChanges, far more than a person walking between places makes.
/// - Ping-pong: back and forth between the same two zones PingPongs times in a row, each
///   stay shorter than QuickStay: bounced straight back. A visit to the shop and one to
///   the college (town, shop, town, college, town) is play, not this.
/// </summary>
public sealed class ZoneChanges
{
    public const double Window = 60;
    public const int MaxChanges = 8;
    public const int PingPongs = 3;
    public const double QuickStay = 10;

    private readonly List<ZoneChange> _changes = new List<ZoneChange>();

    public IReadOnlyList<ZoneChange> Recent
    {
        get { return _changes; }
    }

    public void Add(double time, string from, string to)
    {
        _changes.Add(new ZoneChange(time, from, to));

        while (_changes.Count > 0 && _changes[0].Time < time - Window)
        {
            _changes.RemoveAt(0);
        }
    }

    public bool Churning
    {
        get { return _changes.Count > MaxChanges; }
    }

    // How many of the latest changes, in a row, go between the same two zones and turn
    // straight back: A to B, B to A, A to B.
    public int BackAndForth()
    {
        int count = 0;

        for (int i = _changes.Count - 1; i > 0; i--)
        {
            ZoneChange later = _changes[i];
            ZoneChange earlier = _changes[i - 1];

            if (later.From != earlier.To || later.To != earlier.From || later.Time - earlier.Time > QuickStay)
            {
                break;
            }

            count++;
        }

        return count;
    }

    public bool PingPonging
    {
        get { return BackAndForth() >= PingPongs; }
    }
}

/// <summary>
/// An emote typed in chat, as its player sees it: with nothing in the way (not online, no
/// screen open) the body does it; online, where the server holds the body still, it does
/// not. With a screen open the chat key may not reach the chat line, so that is not judged.
/// </summary>
public static class EmoteCheck
{
    public static string? Judge(string emote, bool clear, bool online, bool seen)
    {
        if (clear && !seen)
        {
            return "typed /" + emote + " with nothing in the way, and the body did not do it within 2 s";
        }

        if (online && seen)
        {
            return "typed /" + emote + " while online, and the body did it";
        }

        return null;
    }
}
