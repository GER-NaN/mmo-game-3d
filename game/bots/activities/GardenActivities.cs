namespace MmoGame3d.Bots;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Gardening;
using MmoGame3d.Interact;
using MmoGame3d.Rules.World;

/// <summary>
/// Activities at the greenhouse: a house plant made at the potting table, pieces planted,
/// named and completed, the table saying it was made and the reward in the bag. And the
/// plants on display in the outskirts, one inspected.
/// </summary>
public static class GardenActivities
{
    private static readonly string[] Names = { "Fern", "Spike", "Office jungle", "Kevin", "The tall one", "Leafy", "" };

    public static List<BotActivity> All()
    {
        return new List<BotActivity>
        {
            HousePlant(),

            new BotActivity("inspect-a-plant", plan => plan
                .InWorld()
                .GoTo(ZoneIds.Outskirts)
                .Wait(2)
                .StopIf("no plants on display", body => !AnyOnDisplay(body))
                .Use<DisplayPlant>()
                .UntilOpen("plant-card")
                .Wait(2)
                .Click<PlantCard>("%Close")
                .UntilClosed("plant-card"))
                .Says("Look at this one.", "Who made that?"),
        };
    }

    private static bool AnyOnDisplay(BotBody body)
    {
        Node? things = body.Zone?.GetNodeOrNull(Interactable.ParentName);

        if (things == null)
        {
            return false;
        }

        foreach (Node child in things.GetChildren())
        {
            if (child is DisplayPlant)
            {
                return true;
            }
        }

        return false;
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
