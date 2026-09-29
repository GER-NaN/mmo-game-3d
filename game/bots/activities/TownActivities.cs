namespace MmoGame3d.Bots;

using System.Collections.Generic;
using MmoGame3d.College;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Town;
using MmoGame3d.Rules.World;
using MmoGame3d.Subway;
using MmoGame3d.Taxis;
using MmoGame3d.Town;
using MmoGame3d.Ui;

/// <summary>
/// Activities with the town and its people: fixing what is broken, the street lights'
/// repair job end to end, small talk with a townsperson, a robo taxi ride, the subway's
/// wall and visitor book, the college's registrar and professor. Where the town has
/// nothing for one to do (nothing broken, the lights working, the taxis down), it stops
/// there as completed.
/// </summary>
public static class TownActivities
{
    public static List<BotActivity> All()
    {
        return new List<BotActivity>
        {
            new BotActivity("fix-something", plan => plan
                .InWorld()
                .GoTo(ZoneIds.Town)
                .StopIf("nothing is broken", body => !AnythingBroken(body))
                .Use<Fixable>("broken", fixable => fixable.Broken)
                .Until("fixed, by it or by another", body => !((body.LastApproached as Fixable)?.Broken ?? false), 10))
                .Says("I can fix that.", "Good as new."),

            RepairLights(),

            new BotActivity("talk-to-townsperson", plan => plan
                .InWorld()
                .GoTo(ZoneIds.Town)
                .Use<Townsperson>()
                .UntilNotice(":"))
                .Says("Nice day, isn't it?", "Hello there!"),

            new BotActivity("ride-taxi", plan => plan
                .InWorld()
                .GoTo(ZoneIds.Town)
                .StopIf("the taxis are out of service", body => !(Town(body)?.TaxisClean ?? false))
                .Use<TaxiStand>()
                .Until("in a taxi", body => body.Zone != null && ZoneIds.SceneOf(body.Zone.ZoneId) == ZoneIds.Taxi, 30)
                .Until("dropped off in town", body => body.Zone != null && body.Zone.ZoneId == ZoneIds.Town, 120))
                .Says("Taxi!", "Let's go for a ride."),

            // Stands by the taxi stand a while, as someone waiting for a ride: near enough
            // to ride along when a party member calls one.
            new BotActivity("wait-for-a-taxi", plan => plan
                .InWorld()
                .GoTo(ZoneIds.Town)
                .Step(new ApproachStep<TaxiStand>("", stand => true))
                .Wait(60))
                .Says("Waiting for a ride."),

            // Counted before the press: the answer can come back within the key press.
            new BotActivity("tag-subway", plan =>
            {
                int before = 0;

                return plan
                    .InWorld()
                    .GoTo(ZoneIds.Subway)
                    .Do("count the notices", body => before = body.View?.NoticeCount ?? 0)
                    .Use<SubwayWallNode>()
                    .Until("told the name is on the wall", body => body.NoticesSince(before).Exists(notice => notice.Contains("on the wall")), 10);
            }).Says("Leaving my mark.", "Art!"),

            new BotActivity("equip-emp", new[] { BotFacts.EmpEquipped }, plan => plan
                .InWorld()
                .StopIf("the EMP emitter is on already", BotFacts.EmpEquipped.Holds)
                .Need(BotFacts.Carrying(ItemType.EmpEmitter))
                .Press("inventory")
                .UntilOpen("inventory")
                .Click("Equip on the EMP emitter", body => BotScreens.RowButton(body.Find<InventoryPanel>()?.GetNode("%Things"), ItemType.EmpEmitter + "_", "Equip"))
                .Until(BotFacts.EmpEquipped)
                .Press("inventory")
                .UntilClosed("inventory")),

            new BotActivity("hunt-drone", plan => plan
                .InWorld()
                .GoTo(ZoneIds.Town)
                .StopIf("no drone is flying", body => !AnyDroneFlying(body))
                .Need(BotFacts.EmpEquipped)
                .GoTo(ZoneIds.Town)
                .Step(new HuntStep()))
                .Says("Drone spotted!", "Get down here."),

            new BotActivity("take-the-class", plan => plan
                .InWorld()
                .GoTo(ZoneIds.College)
                .Use<CollegePerson>("Registrar", who => who.Name == "Registrar")
                .UntilOpen("college")
                .StopIf("the class is taken", body => BotScreens.FirstButton(body.Find<CollegePanel>()!, "Finish the Class") == null)
                .Click("Finish the Class", body => BotScreens.FirstButton(body.Find<CollegePanel>()!, "Finish the Class"))
                .Wait(1)
                .Press("ui_cancel")
                .UntilClosed("college"))
                .Says("Back to school."),

            new BotActivity("enroll", plan => plan
                .InWorld()
                .Then("take-the-class")
                .GoTo(ZoneIds.College)
                .Use<CollegePerson>("Registrar", who => who.Name == "Registrar")
                .UntilOpen("college")
                .StopIf("no career open to it", body => EnrollButton(body) == null)
                .Click("a career", EnrollButton)
                .UntilNotice("", 5)
                .Press("ui_cancel")
                .UntilClosed("college"))
                .Says("Time for a career.", "Signing up."),

            CleanTaxis(),
            TalkTo("Registrar"),
            TalkTo("Professor"),
        };
    }

    // The street lights' repair job, end to end (first-playable.md): with the part in the
    // bag (a RAM stick, bought if need be), the job taken in a terminal's Town repairs,
    // the junction box repaired, and the lights seen to work.
    private static BotActivity RepairLights()
    {
        return new BotActivity("repair-lights", plan => plan
            .InWorld()
            .GoTo(ZoneIds.Town)
            .StopIf("the lights work", body => Town(body)?.LightsWorking ?? true)
            .Need(BotFacts.Carrying(ItemType.RamStick))
            .GoTo(ZoneIds.Town)
            .Step(new UseTerminalStep())
            .Click("the Town repairs app", body => BotScreens.TerminalApp(body.Find<TerminalScreen>(), "Town repairs"))
            .ClickIfThere("Take the job (the lights)", body => BotScreens.Named<Godot.Button>(body.Find<TerminalScreen>(), "Take_" + StreetLights.JobId))
            .Wait(1)
            .Press("ui_cancel")
            .UntilClosed("terminal")
            .Use<JunctionBox>()
            .Until("the lights work", body => Town(body)?.LightsWorking ?? false, 10))
            .Says("Let there be light.", "Fixing the street lights.");
    }

    // The robo taxis' rootkit job, end to end: the job taken in Town repairs, a code
    // cracked in the code cracker (which is the cleaning), and the taxis seen clean.
    private static BotActivity CleanTaxis()
    {
        return new BotActivity("clean-taxis", plan => plan
            .InWorld()
            .GoTo(ZoneIds.Town)
            .StopIf("the taxis are clean", body => Town(body)?.TaxisClean ?? true)
            .Step(new UseTerminalStep())
            .Click("the Town repairs app", body => BotScreens.TerminalApp(body.Find<TerminalScreen>(), "Town repairs"))
            .ClickIfThere("Take the job (the taxis)", body => BotScreens.Named<Godot.Button>(body.Find<TerminalScreen>(), "Take_" + TaxiRootkit.JobId))
            .Click("the Code cracker app", body => BotScreens.TerminalApp(body.Find<TerminalScreen>(), "Code cracker"))
            .Click("New code", body => BotScreens.FirstButton(body.Find<TerminalScreen>()!, "New code"))
            .Step(new CrackStep())
            .Wait(0.5)
            .Press("ui_cancel")
            .UntilClosed("terminal")
            .Until("the taxis are clean", body => Town(body)?.TaxisClean ?? false, 10))
            .Says("Cleaning the rootkit out of the taxis.", "No more AI in the cabs.");
    }

    // Talks to someone at the college: their panel opens, and closes again.
    private static BotActivity TalkTo(string person)
    {
        return new BotActivity("talk-to-" + person.ToLowerInvariant(), plan => plan
            .InWorld()
            .GoTo(ZoneIds.College)
            .Use<CollegePerson>(person, who => who.Name == person)
            .UntilOpen("college")
            .Wait(1)
            .Press("ui_cancel")
            .UntilClosed("college"))
            .Says("I have a question about classes.");
    }

    // The first career the player can take: "Enroll as ..." or "Change to ...", enabled.
    private static Godot.Button? EnrollButton(BotBody body)
    {
        CollegePanel? panel = body.Find<CollegePanel>();
        Godot.Button? enroll = BotScreens.FirstButtonStarting(panel, "Enroll as");
        Godot.Button? change = BotScreens.FirstButtonStarting(panel, "Change to");
        Godot.Button? pick = enroll ?? change;
        return pick != null && !pick.Disabled ? pick : null;
    }

    private static bool AnyDroneFlying(BotBody body)
    {
        Godot.Node? drones = body.Zone?.GetNodeOrNull("Drones");

        if (drones == null)
        {
            return false;
        }

        foreach (Godot.Node child in drones.GetChildren())
        {
            Drones.Drone? drone = child as Drones.Drone;

            if (drone != null && !drone.Down)
            {
                return true;
            }
        }

        return false;
    }

    private static TownState? Town(BotBody body)
    {
        return body.InGroup<TownState>(TownState.Group);
    }

    private static bool AnythingBroken(BotBody body)
    {
        Godot.Node? things = body.Zone?.GetNodeOrNull(Interact.Interactable.ParentName);

        if (things == null)
        {
            return false;
        }

        foreach (Godot.Node child in things.GetChildren())
        {
            Fixable? fixable = child as Fixable;

            if (fixable != null && fixable.Broken)
            {
                return true;
            }
        }

        return false;
    }
}
