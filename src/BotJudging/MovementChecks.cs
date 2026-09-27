namespace MmoGame3d.BotJudging;

using System.Collections.Generic;
using System.Numerics;

/// <summary>What a thrashing check found: the numbers for the finding's line.</summary>
public sealed class ThrashVerdict
{
    public ThrashVerdict(int reversals, float travelled, float net, float widestGap)
    {
        Reversals = reversals;
        Travelled = travelled;
        Net = net;
        WidestGap = widestGap;
    }

    public int Reversals { get; }

    public float Travelled { get; }

    public float Net { get; }

    public float WidestGap { get; }
}

/// <summary>
/// Back and forth in one place: sharp reversals, each move against the one before, with
/// little gained, over a few seconds. A walk in a circle has none; a body jittering a few
/// centimetres as it turns in a gap has none either, since a move counts from 1 m/s.
/// </summary>
public static class Thrashing
{
    public const double Every = 0.25;
    public const double Window = 5;
    public const int Reversals = 4;
    public const float Net = 1.5f;
    public const float MinMove = 0.25f;

    // Null when the track (Every apart, Window long) is not thrashing.
    public static ThrashVerdict? Judge(IReadOnlyList<TrackSample> track)
    {
        if (track.Count < 2 || track[track.Count - 1].Time - track[0].Time < Window - Every)
        {
            return null;
        }

        float travelled = 0f;
        float widest = 0f;
        int reversals = 0;

        for (int i = 0; i < track.Count; i++)
        {
            widest = System.Math.Max(widest, track[i].Gap);

            if (i < 1)
            {
                continue;
            }

            Vector3 move = TrackSample.Flat(track[i].At - track[i - 1].At);
            travelled += move.Length();

            if (i < 2)
            {
                continue;
            }

            Vector3 before = TrackSample.Flat(track[i - 1].At - track[i - 2].At);

            if (move.Length() > MinMove && before.Length() > MinMove && Vector3.Dot(Vector3.Normalize(move), Vector3.Normalize(before)) < -0.5f)
            {
                reversals++;
            }
        }

        float net = TrackSample.Flat(track[track.Count - 1].At - track[0].At).Length();
        return reversals >= Reversals && net < Net ? new ThrashVerdict(reversals, travelled, net, widest) : null;
    }
}

/// <summary>
/// Not getting anywhere while meaning to: every look in the last StuckAfter seconds within
/// Radius of where the body is now, and most of that time walking. Standing still at a
/// terminal or a panel is not walking, so it is not stuck.
/// </summary>
public static class Stuck
{
    public const float Radius = 2.5f;
    public const double After = 30;
    public const double WalkingShare = 0.9;

    public static bool Judge(IReadOnlyList<TrackSample> history, Vector3 now, double clock)
    {
        if (history.Count == 0 || clock - history[0].Time < After)
        {
            return false;
        }

        int walking = 0;

        foreach (TrackSample sample in history)
        {
            if (Vector3.Distance(sample.At, now) > Radius)
            {
                return false;
            }

            if (sample.Walking)
            {
                walking++;
            }
        }

        return walking >= history.Count * WalkingShare;
    }
}

/// <summary>
/// A traveller as its player sees it: not moving, and not in the zone it is going to, for
/// a good while, is stuck. Moving, a new zone, or a ride (a taxi's cabin, where a rider
/// sits still) starts the clock again.
/// </summary>
public sealed class TravelWatch
{
    public const double StillFor = 25;
    public const float Moved = 1.5f;

    private readonly string _to;
    private Vector3 _from;
    private string _zone = "";
    private double _still;
    private bool _started;

    public TravelWatch(string to)
    {
        _to = to;
    }

    // A look after delta seconds; the reason it is stuck, or null.
    public string? Look(double delta, string zone, Vector3 at, bool riding = false)
    {
        if (!_started || riding || zone != _zone || Vector3.Distance(at, _from) > Moved)
        {
            _started = true;
            _zone = zone;
            _from = at;
            _still = 0;
            return null;
        }

        if (zone == _to)
        {
            return null;
        }

        _still += delta;
        return _still >= StillFor ? "not moving for " + (int)StillFor + " s, and still in " + zone + " on the way to " + _to : null;
    }
}
