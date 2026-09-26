namespace MmoGame3d.Rules.Terminals;

using System;
using System.Collections.Generic;

/// <summary>
/// Agent Defense, the first Defense Objective (world.md): a rhythm task in the manner of
/// Guitar Hero and piano tiles. Cues come down four lanes and the player presses each
/// lane's key as a cue crosses the line; in the fiction they are cutting the AI's hold on
/// the grid, one line at a time. About a minute, single player, and deterministic after
/// its seed: the server picks the seed, the client plays it and sends its key presses,
/// and the server plays the same chart against them for the score, so a top score can
/// be played back later.
/// </summary>
public static class AgentDefense
{
    public const int Lanes = 4;
    public const int LengthMs = 60000;

    // Placeholders, to be tuned by playing: how far off a press may be and still count,
    // and what each is worth.
    public const int PerfectMs = 60;
    public const int GoodMs = 130;
    public const int PerfectPoints = 100;
    public const int GoodPoints = 50;

    // The combo multiplies points: x1, then x2 from 10 in a row, x3 from 25, x4 from 50.
    public static int Multiplier(int combo)
    {
        if (combo >= 50)
        {
            return 4;
        }

        if (combo >= 25)
        {
            return 3;
        }

        return combo >= 10 ? 2 : 1;
    }

    // The cues, in time order: starting sparse, getting busier towards the end, now and
    // then two at once. The same seed always gives the same chart.
    public static List<DefenseCue> Chart(int seed)
    {
        return Chart(seed, LengthMs);
    }

    // A run of another length: dev tests play short ones.
    public static List<DefenseCue> Chart(int seed, int lengthMs)
    {
        Random random = new Random(seed);
        List<DefenseCue> cues = new List<DefenseCue>();
        int at = 2000;

        while (at < lengthMs - 1500)
        {
            float progress = at / (float)lengthMs;
            int gap = (int)(700 - (400 * progress));
            int lane = random.Next(Lanes);
            cues.Add(new DefenseCue(at, lane));

            if (progress > 0.4f && random.NextDouble() < 0.15)
            {
                cues.Add(new DefenseCue(at, (lane + 1 + random.Next(Lanes - 1)) % Lanes));
            }

            at += gap + (random.Next(3) * 60);
        }

        return cues;
    }

    // Plays the chart against the presses (time in ms from the start, lane). Each press
    // takes the nearest cue in its lane still unhit within reach; a press with none is
    // wasted and breaks the combo, and so does a cue nobody hit.
    public static DefenseResult Score(List<DefenseCue> chart, IReadOnlyList<DefensePress> presses)
    {
        bool[] hit = new bool[chart.Count];
        List<DefensePress> ordered = new List<DefensePress>(presses);
        ordered.Sort((a, b) => a.AtMs.CompareTo(b.AtMs));
        DefenseResult result = new DefenseResult { Cues = chart.Count };
        int next = 0;
        int combo = 0;

        foreach (DefensePress press in ordered)
        {
            // Cues too old to be hit by this press or any later one are misses.
            while (next < chart.Count && chart[next].AtMs < press.AtMs - GoodMs)
            {
                if (!hit[next])
                {
                    combo = 0;
                }

                next++;
            }

            int best = -1;
            int bestOff = GoodMs + 1;

            for (int i = next; i < chart.Count && chart[i].AtMs <= press.AtMs + GoodMs; i++)
            {
                int off = Math.Abs(chart[i].AtMs - press.AtMs);

                if (!hit[i] && chart[i].Lane == press.Lane && off < bestOff)
                {
                    best = i;
                    bestOff = off;
                }
            }

            if (best < 0)
            {
                combo = 0;
                continue;
            }

            hit[best] = true;
            combo++;
            result.BestCombo = Math.Max(result.BestCombo, combo);
            bool perfect = bestOff <= PerfectMs;
            result.Perfect += perfect ? 1 : 0;
            result.Good += perfect ? 0 : 1;
            result.Points += (perfect ? PerfectPoints : GoodPoints) * Multiplier(combo);
        }

        return result;
    }

    // Presses travel as one int each: time in ms times ten, plus the lane.
    public static int Pack(DefensePress press)
    {
        return (press.AtMs * 10) + press.Lane;
    }

    public static List<DefensePress> Unpack(int[] packed)
    {
        List<DefensePress> presses = new List<DefensePress>();

        foreach (int value in packed)
        {
            int lane = value % 10;
            int at = value / 10;

            if (value >= 0 && lane < Lanes && at <= LengthMs + 2000)
            {
                presses.Add(new DefensePress(at, lane));
            }
        }

        return presses;
    }
}

public class DefenseCue
{
    public DefenseCue(int atMs, int lane)
    {
        AtMs = atMs;
        Lane = lane;
    }

    public int AtMs { get; }
    public int Lane { get; }
}

public class DefensePress
{
    public DefensePress(int atMs, int lane)
    {
        AtMs = atMs;
        Lane = lane;
    }

    public int AtMs { get; }
    public int Lane { get; }
}

public class DefenseResult
{
    public int Cues { get; set; }
    public int Perfect { get; set; }
    public int Good { get; set; }
    public int BestCombo { get; set; }
    public int Points { get; set; }

    public int Missed
    {
        get { return Cues - Perfect - Good; }
    }
}
