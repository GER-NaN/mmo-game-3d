namespace MmoGame3d.Bots;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Rules.World;
using MmoGame3d.Terminals;
using MmoGame3d.Ui;

/// <summary>
/// Activities of moving about the world: jumping, walking, going through doors, and
/// surveying a town's map. The ones for a town are made per town, so a new town is a line
/// more (Surveyor, Surveillance).
/// </summary>
public static class WorldActivities
{
    public static List<BotActivity> All()
    {
        return new List<BotActivity>
        {
            Jump(),
            Meadows(),

            new BotActivity("old-town-explorer", plan => plan
                .InWorld()
                .Enter("college")
                .Wait(3)
                .Enter("subway")
                .Wait(3)
                .Enter("shop")
                .Wait(3))
                .Says("This part of town looks different.", "Never been in here before.", "Where does this door go?"),

            // New Town has no terminal: this one fails, and should say so plainly.
            new BotActivity("new-town-terminal", plan => plan
                .InWorld()
                .GoTo(ZoneIds.NewTown)
                .Use<Terminal>()
                .WaitFor<TerminalScreen>()),

            Surveyor(ZoneIds.Town),
            Surveyor(ZoneIds.NewTown),
            Surveillance(ZoneIds.Town),
            Surveillance(ZoneIds.NewTown),
        };
    }

    // Jumps once and sees the server's body rise.
    private static BotActivity Jump()
    {
        return new BotActivity("jump", plan =>
        {
            float ground = 0;
            float highest = 0;

            return plan
                .InWorld()
                .Wait(2)
                .Do("note the ground", body => { ground = body.Player!.NetPosition.Y; highest = ground; })
                .Press("jump")
                .Until("the server's body rose 0.5 m", body =>
                {
                    highest = Mathf.Max(highest, body.Player?.NetPosition.Y ?? highest);
                    return highest - ground >= 0.5f;
                }, 3)
                .Do("report", body => body.Events.Write("rose", (highest - ground).ToString("0.00") + " m"));
        }).Says("Hup!", "Did anyone see that jump?");
    }

    // Walks in through the meadows door, then a few seconds forward.
    private static BotActivity Meadows()
    {
        return new BotActivity("meadows", plan =>
        {
            Vector3 start = Vector3.Zero;

            return plan
                .InWorld()
                .Enter(ZoneIds.Meadows)
                .Do("note the start", body => start = body.Player!.NetPosition)
                .Hold("move_forward", 3)
                .Until("walked 3 m", body => new Vector2(body.Player!.NetPosition.X - start.X, body.Player.NetPosition.Z - start.Z).Length() >= 3f, 2);
        }).Says("Off to the meadows.", "Fresh air out here.", "The grass is taller than I thought.");
    }

    // Walks a town until its whole map is discovered.
    private static BotActivity Surveyor(string zone)
    {
        return new BotActivity("surveyor-" + zone, plan => plan
            .InWorld()
            .GoTo(zone)
            .Survey())
            .Says("Where do I go next?", "Another corner mapped.", "Almost got this place on the map.");
    }

    // Surveys a town, then watches its cameras from the nearest terminal, reports a drone
    // and sees the town pay for it.
    private static BotActivity Surveillance(string zone)
    {
        return new BotActivity("surveillance-" + zone, plan =>
        {
            int dollars = 0;

            return plan
                .InWorld()
                .GoTo(zone)
                .Survey()
                .Use<Terminal>()
                .WaitFor<TerminalScreen>()
                .Click("the Town cameras app", body => BotScreens.TerminalApp(body.Find<TerminalScreen>(), "Town cameras"))
                .WaitFor<CctvView>()
                .Do("note the money", body => dollars = body.View!.Dollars)
                .WatchForDrones(1, 180)
                .Until("paid for the report", body => body.View!.Dollars > dollars);
        }).Says("Walking the beat.", "Eyes on the cameras.", "Quiet night so far.");
    }
}
