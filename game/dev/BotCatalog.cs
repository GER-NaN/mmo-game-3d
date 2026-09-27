namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;

/// <summary>
/// A game feature's part in bot testing, in one file: the activities and goals that play
/// it. Every class that implements this is found when bots start (BotCatalog) and adds
/// its own; nothing else needs changing. See game/dev/features/.
/// </summary>
public interface IBotFeature
{
    void AddTo(BotCatalog catalog);
}

/// <summary>
/// Everything a bot can pick: the activities and goals in BotActivities and BotGoals,
/// and those every IBotFeature adds.
/// </summary>
public sealed class BotCatalog
{
    private static BotCatalog? _all;

    public List<BotActivity> Activities { get; } = new List<BotActivity>();

    public List<BotGoal> Goals { get; } = new List<BotGoal>();

    public static BotCatalog All
    {
        get
        {
            if (_all == null)
            {
                _all = new BotCatalog();
                _all.Activities.AddRange(BotActivities.All);
                _all.Goals.AddRange(BotGoals.All);

                foreach (Type type in typeof(IBotFeature).Assembly.GetTypes())
                {
                    if (typeof(IBotFeature).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                    {
                        ((IBotFeature)Activator.CreateInstance(type)!).AddTo(_all);
                    }
                }
            }

            return _all;
        }
    }

    public void Add(BotActivity activity)
    {
        Activities.Add(activity);
    }

    public void Add(BotGoal goal)
    {
        Goals.Add(goal);
    }
}
