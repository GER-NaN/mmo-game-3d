namespace MmoGame3d.BotJudging;

using System.Collections.Generic;
using System.Numerics;

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
