namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;

/// <summary>
/// Works a goal backward to the next thing to do: for the first wanted fact that does not
/// hold, an activity that gives it (any one of those that do, at random: variety tests more
/// than the shortest way), and for that activity's first need that does not hold, the same
/// again. "In a zone" is the router's (TravelActivity), and an activity's own zone is the
/// driver's, which routes there before starting it. Asked again after every activity, so a
/// cancel, a failure or a party pull only changes where the next answer starts from.
/// </summary>
public static class BotResolver
{
    private const int MaxDepth = 5;

    // The next activity toward the facts wanted, or null: all hold (why ""), or they
    // cannot be reached (why says which need nothing gives). plan reads the chain of
    // reasons, for the log.
    public static BotActivity? Next(BotBody body, IReadOnlyList<BotFact> wanted, IReadOnlyList<BotActivity> providers, Random random, out string why, out string plan)
    {
        why = "";
        plan = "";

        foreach (BotFact fact in wanted)
        {
            if (fact.IsTrue(body))
            {
                continue;
            }

            List<string> reasons = new List<string>();
            BotActivity? next = Resolve(body, fact, providers, random, 0, reasons, out why);
            plan = string.Join(" <- ", reasons);
            return next;
        }

        return null;
    }

    private static BotActivity? Resolve(BotBody body, BotFact fact, IReadOnlyList<BotActivity> providers, Random random, int depth, List<string> reasons, out string why)
    {
        why = "";
        reasons.Add(fact.ToString());

        if (depth >= MaxDepth)
        {
            why = "more than " + MaxDepth + " steps deep at " + fact;
            return null;
        }

        if (fact.Kind == FactKind.InZone)
        {
            TravelActivity travel = new TravelActivity(fact.Key);
            reasons.Add(travel.Name);
            return travel;
        }

        List<BotActivity> givers = new List<BotActivity>();

        foreach (BotActivity provider in providers)
        {
            if (provider.Timing != BotTiming.Chosen || !provider.CanStart(body))
            {
                continue;
            }

            foreach (BotFact given in provider.Gives)
            {
                if (given.Answers(fact))
                {
                    givers.Add(provider);
                    break;
                }
            }
        }

        if (givers.Count == 0)
        {
            why = "nothing here gives " + fact;
            return null;
        }

        BotActivity pick = givers[random.Next(givers.Count)];
        reasons.Add(pick.Name);

        foreach (BotFact need in pick.Needs)
        {
            if (!need.IsTrue(body))
            {
                return Resolve(body, need, providers, random, depth + 1, reasons, out why);
            }
        }

        return pick;
    }
}
