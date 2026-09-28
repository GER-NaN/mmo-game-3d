namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Dev.Activities;

/// <summary>
/// The --bot-everything run: every activity there is, in a shuffled order, each to its
/// end, then a tally and a new shuffle. Static, so it outlives the BotDriver, which is
/// made again at each login (a character switch is one of the activities).
/// </summary>
public static class EverythingRun
{
    private static readonly List<BotActivity> Queue = new List<BotActivity>();
    private static readonly List<string> Finished = new List<string>();
    private static readonly List<string> Failed = new List<string>();
    private static readonly List<string> CouldNotStart = new List<string>();
    private static int _round;
    private static int _roundSize;

    // What it is on now, until it ends one way or another.
    public static BotActivity? Current { get; private set; }

    // The next one, with the last round's tally first when a round is over.
    public static BotActivity Next(Random random)
    {
        if (Queue.Count == 0)
        {
            if (_round > 0)
            {
                PrintTally();
            }

            Fill(random);
        }

        Current = Queue[0];
        Queue.RemoveAt(0);
        GD.Print("Bot: everything, round " + _round + ", " + (_roundSize - Queue.Count) + " of " + _roundSize + ": \"" + Current.Name + "\"");
        return Current;
    }

    public static void Ended(bool finished, string why)
    {
        if (Current == null)
        {
            return;
        }

        if (finished)
        {
            Finished.Add(Current.Name);
        }
        else
        {
            Failed.Add(Current.Name + " (" + why + ")");
        }

        Current = null;
    }

    public static void NotStarted(string why)
    {
        if (Current == null)
        {
            return;
        }

        CouldNotStart.Add(Current.Name + " (" + why + ")");
        Current = null;
    }

    private static void Fill(Random random)
    {
        List<BotActivity> all = new List<BotActivity>(BotCatalog.All.Activities);
        all.AddRange(PersonaActivities.All);
        HashSet<string> seen = new HashSet<string>();

        foreach (BotActivity activity in all)
        {
            if (seen.Add(activity.Name))
            {
                Queue.Add(activity);
            }
        }

        for (int i = Queue.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            BotActivity swap = Queue[i];
            Queue[i] = Queue[j];
            Queue[j] = swap;
        }

        _round++;
        _roundSize = Queue.Count;
        Finished.Clear();
        Failed.Clear();
        CouldNotStart.Clear();
        GD.Print("Bot: everything, round " + _round + ": " + _roundSize + " activities");
    }

    private static void PrintTally()
    {
        GD.Print("Bot: everything, round " + _round + " over: " + Finished.Count + " finished, " + Failed.Count + " gave up, " + CouldNotStart.Count + " could not start");

        foreach (string failed in Failed)
        {
            GD.Print("Bot:   gave up on " + failed);
        }

        foreach (string skipped in CouldNotStart)
        {
            GD.Print("Bot:   could not start " + skipped);
        }
    }
}
