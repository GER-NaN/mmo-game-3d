namespace MmoGame3d.BotJudging;

using System.Collections.Generic;
using System.Numerics;

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
