namespace MmoGame3d.Bots;

using System.Collections.Generic;
using MmoGame3d.Rules.World;

/// <summary>
/// Activities that look for what nobody planned for (bots.md F1, R3): running for the
/// zone's edge, squeezing into a building's corner, mashing keys, poking at whatever
/// buttons are on screen. They pass by finishing; what they are for is what the watchers
/// record meanwhile (out of bounds, floating, stuck, client errors).
/// </summary>
public static class ChaosActivities
{
    public static List<BotActivity> All()
    {
        return new List<BotActivity>
        {
            Anywhere("run-for-the-edge", plan => plan.Step(new EdgeRunStep()))
                .Says("I wonder what's past the edge.", "Keep running!"),

            new BotActivity("squeeze-into-a-corner", plan => plan
                .InWorld()
                .GoTo(ZoneIds.Town)
                .Step(new WedgeStep()))
                .Says("Can I fit in here?"),

            Anywhere("mash-keys", plan => plan.Step(new MashStep()))
                .Says("asdfghjkl"),

            Anywhere("poke-around", plan => plan.Step(new PokeStep()))
                .Says("What does this button do?"),
        };
    }

    // Wherever the bot is: in the world, then the steps.
    private static BotActivity Anywhere(string name, System.Func<BotPlan, BotPlan> steps)
    {
        return new BotActivity(name, plan => steps(plan.InWorld()));
    }
}
