namespace MmoGame3d.Bots;

using System.Collections.Generic;

/// <summary>
/// The registry: every activity bots can do, by name, written by hand (bots.md T9a), one
/// area of the game to a file under activities/. A new area adds its line here; a new
/// activity goes in its area's file. bot.json and the personas name activities.
/// </summary>
public static class BotActivities
{
    public static readonly List<BotActivity> All = Gather();

    public static BotActivity? Named(string name)
    {
        foreach (BotActivity activity in All)
        {
            if (activity.Name == name)
            {
                return activity;
            }
        }

        return null;
    }

    // Every activity that makes true a fact with this key.
    public static List<BotActivity> ProvidersOf(string key)
    {
        List<BotActivity> providers = new List<BotActivity>();

        foreach (BotActivity activity in All)
        {
            foreach (string provided in activity.Provides)
            {
                if (provided == key)
                {
                    providers.Add(activity);
                    break;
                }
            }
        }

        return providers;
    }

    private static List<BotActivity> Gather()
    {
        List<BotActivity> all = new List<BotActivity>();
        all.AddRange(MenuActivities.All());
        all.AddRange(WorldActivities.All());
        all.AddRange(PhoneActivities.All());
        all.AddRange(UiActivities.All());
        return all;
    }
}
