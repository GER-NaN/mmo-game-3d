namespace MmoGame3d.Bots;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.World;
using MmoGame3d.Terminals;
using MmoGame3d.Ui;

/// <summary>
/// The registry: every activity bots can do, by name, written by hand (bots.md T9a). A
/// new feature adds its activity here. bot.json names the one a bot runs.
/// </summary>
public static class BotActivities
{
    public static readonly List<BotActivity> All = new List<BotActivity>
    {
        new BotActivity("main-menu", plan => plan
            .WaitFor<MainMenu>()),

        new BotActivity("quit", plan => plan
            .WaitFor<MainMenu>()
            .Click<MainMenu>("%Quit")
            .ExpectQuit()),

        new BotActivity("fullscreen", plan => plan
            .WaitFor<MainMenu>()
            .Click<MainMenu>("%Settings")
            .WaitFor<SettingsPanel>()
            .Click<SettingsPanel>("%Fullscreen")
            .Until(BotFacts.Fullscreen)
            .Click<SettingsPanel>("%Fullscreen")
            .Until("windowed", body => !BotFacts.Fullscreen.Holds(body))),

        new BotActivity("jump", plan =>
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
        }).Says("Hup!", "Did anyone see that jump?"),

        new BotActivity("meadows", plan =>
        {
            Vector3 start = Vector3.Zero;

            return plan
                .InWorld()
                .Enter("meadows")
                .Do("note the start", body => start = body.Player!.NetPosition)
                .Hold("move_forward", 3)
                .Until("walked 3 m", body => Flat(body.Player!.NetPosition, start) >= 3f, 2);
        }).Says("Off to the meadows.", "Fresh air out here.", "The grass is taller than I thought."),

        new BotActivity("old-town-explorer", plan => plan
            .InWorld()
            .Enter("college")
            .Wait(3)
            .Enter("subway")
            .Wait(3)
            .Enter("shop")
            .Wait(3))
            .Says("This part of town looks different.", "Never been in here before.", "Where does this door go?"),

        new BotActivity("equip-phone", new[] { BotFacts.PhoneEquipped }, plan => plan
            .InWorld()
            .Wait(1)
            .Press("inventory")
            .WaitFor<InventoryPanel>()
            .Click("Equip on the phone", body => BotScreens.EquipButton(body.Find<InventoryPanel>(), ItemType.Phone))
            .Until(BotFacts.PhoneEquipped)
            .Press("inventory"))
            .Says("Where did I put my phone?", "Phone's on."),

        new BotActivity("phone-terminal", plan => plan
            .InWorld()
            .Wait(1)
            .Need(BotFacts.PhoneEquipped)
            .Press("phone")
            .WaitFor<TerminalScreen>())
            .Says("Checking my phone.", "Anyone else online?"),

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

    // Walks a town until its whole map is discovered. One entry per town.
    private static BotActivity Surveyor(string zone)
    {
        return new BotActivity("surveyor-" + zone, plan => plan
            .InWorld()
            .GoTo(zone)
            .Survey())
            .Says("Where do I go next?", "Another corner mapped.", "Almost got this place on the map.");
    }

    // Surveys a town, then watches its cameras from the nearest terminal, reports a drone
    // and sees the town pay for it. One entry per town.
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

    public static BotActivity? ProviderOf(BotFact fact)
    {
        foreach (BotActivity activity in All)
        {
            foreach (BotFact provided in activity.Provides)
            {
                if (provided == fact)
                {
                    return activity;
                }
            }
        }

        return null;
    }

    private static float Flat(Vector3 a, Vector3 b)
    {
        return new Vector2(a.X - b.X, a.Z - b.Z).Length();
    }
}
