namespace MmoGame3d.Bots;

using System.Collections.Generic;
using MmoGame3d.Gardening;
using MmoGame3d.Rules.World;

/// <summary>
/// Activities at the greenhouse: a house plant made at the potting table, pieces planted,
/// named and completed, the table saying it was made and the reward in the bag.
/// </summary>
public static class GardenActivities
{
    private static readonly string[] Names = { "Fern", "Spike", "Office jungle", "Kevin", "The tall one", "Leafy", "" };

    public static List<BotActivity> All()
    {
        return new List<BotActivity>
        {
            HousePlant(),
        };
    }

    private static BotActivity HousePlant()
    {
        return new BotActivity("house-plant", plan =>
        {
            int before = 0;

            return plan
                .InWorld()
                .GoTo(ZoneIds.Greenhouse)
                .Do("count the bag", body => before = BotFacts.BagCount(body))
                .Use<PottingTable>()
                .UntilOpen("garden")
                .Wait(1)
                .Step(new PlantPiecesStep())
                .Click("Complete", body => BotScreens.FirstButton(body.Find<GardenScreen>()!, "Complete"))
                .Step(new TypeStep("a name", body => Names[body.Random.Next(Names.Length)]))
                .Until("the table says it was made", body => body.Find<GardenScreen>()?.IsDone ?? false, 15)
                .Until("the reward in the bag", body => BotFacts.BagCount(body) > before, 5)
                .Click("Leave the table", body => BotScreens.FirstButton(body.Find<GardenScreen>()!, "Leave the table"))
                .UntilClosed("garden");
        }).Says("Green fingers today.", "Potting something nice.", "This one will be pretty.");
    }
}
