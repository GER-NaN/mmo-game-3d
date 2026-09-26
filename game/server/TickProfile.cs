namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

/// <summary>
/// Where the server's frame goes, for the stats line in load tests: each part of the
/// tick is timed between laps (no allocation per frame), summed and its worst frame kept
/// until the next report; and the worst whole frame, from the gap between frames.
/// </summary>
public class TickProfile
{
    private readonly Dictionary<string, long> _total = new Dictionary<string, long>();
    private readonly Dictionary<string, long> _worst = new Dictionary<string, long>();
    private double _worstFrame;
    private TimeSpan _gcPausedAtReport = GC.GetTotalPauseDuration();
    private int _gen2AtReport = GC.CollectionCount(2);

    public long Start()
    {
        return Stopwatch.GetTimestamp();
    }

    // Charges the time since `from` to `part`; returns now, for the next lap.
    public long Lap(string part, long from)
    {
        long now = Stopwatch.GetTimestamp();
        long spent = now - from;
        long total;
        long worst;
        _total.TryGetValue(part, out total);
        _worst.TryGetValue(part, out worst);
        _total[part] = total + spent;

        if (spent > worst)
        {
            _worst[part] = spent;
        }

        return now;
    }

    public void Frame(double seconds)
    {
        if (seconds > _worstFrame)
        {
            _worstFrame = seconds;
        }
    }

    // "worst frame 18.2 ms; drones 0.40 ms/s (worst 0.2), maps ..." for the parts that
    // took the most, then starts over.
    public string Report(double seconds, int parts)
    {
        List<KeyValuePair<string, long>> ranked = new List<KeyValuePair<string, long>>(_total);
        ranked.Sort((a, b) => b.Value.CompareTo(a.Value));
        StringBuilder text = new StringBuilder();
        text.Append("worst frame ").Append((_worstFrame * 1000.0).ToString("F1", CultureInfo.InvariantCulture)).Append(" ms");

        // The .NET collector's pauses stop the frame too, and are not in any part.
        TimeSpan paused = GC.GetTotalPauseDuration();
        int gen2 = GC.CollectionCount(2);
        text.Append(", gc paused ").Append((paused - _gcPausedAtReport).TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture))
            .Append(" ms (").Append(gen2 - _gen2AtReport).Append(" full)");
        _gcPausedAtReport = paused;
        _gen2AtReport = gen2;

        for (int i = 0; i < ranked.Count && i < parts; i++)
        {
            double perSecond = Milliseconds(ranked[i].Value) / seconds;
            text.Append(i == 0 ? "; " : ", ").Append(ranked[i].Key).Append(' ')
                .Append(perSecond.ToString("F2", CultureInfo.InvariantCulture)).Append(" ms/s (worst ")
                .Append(Milliseconds(_worst[ranked[i].Key]).ToString("F1", CultureInfo.InvariantCulture)).Append(')');
        }

        _total.Clear();
        _worst.Clear();
        _worstFrame = 0;
        return text.ToString();
    }

    private static double Milliseconds(long ticks)
    {
        return ticks * 1000.0 / Stopwatch.Frequency;
    }
}
