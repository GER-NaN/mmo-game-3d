namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Dev.Screens;
using MmoGame3d.Rules.World;
using MmoGame3d.Town;

/// <summary>Walking about Old Town: wandering, a word in chat, the EMP key pressed.</summary>
public sealed class WalkAroundTownActivity : StepsActivity
{
    public WalkAroundTownActivity()
        : base("walk around town", 6)
    {
    }

    public override string Zone
    {
        get { return ZoneIds.Town; }
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new BotPlan()
            .Wander(15 + (body.Random.NextDouble() * 30))
            .Step(ChatUi.SayInPassing(body.Random))
            .Wander(10 + (body.Random.NextDouble() * 20))
            .Step(ScreenSteps.Press("press R for the EMP", "emp"))
            .Steps;
    }
}

/// <summary>The nearest broken thing in the zone (a camera, a signal), fixed.</summary>
public sealed class FixSomethingActivity : StepsActivity
{
    public FixSomethingActivity()
        : base("fix something", 4)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return BotWhere.InWorld(body) && NearestBroken(body) != null;
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep>
        {
            new WalkToStep("walk to the broken thing", NearestBroken),
            new UseStep("Fix the", b => NearestBroken(b) == null || b.DistanceTo(NearestBroken(b)!.GlobalPosition) >= 3f, 10),
        };
    }

    private static Node3D? NearestBroken(BotBody body)
    {
        Node? things = body.Zone?.GetNodeOrNull("Interactables");
        Fixable? nearest = null;

        if (things == null)
        {
            return null;
        }

        foreach (Node node in things.GetChildren())
        {
            Fixable? fixable = node as Fixable;

            if (fixable != null && fixable.Broken && (nearest == null || body.DistanceTo(fixable.GlobalPosition) < body.DistanceTo(nearest.GlobalPosition)))
            {
                nearest = fixable;
            }
        }

        return nearest;
    }
}

/// <summary>Old Town's junction box: the street lights repaired, when they are down.</summary>
public sealed class RepairLightsActivity : StepsActivity
{
    public RepairLightsActivity()
        : base("repair the street lights", 1)
    {
    }

    public override string Zone
    {
        get { return ZoneIds.Town; }
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep>
        {
            new WalkToStep("walk to the junction box", b => b.Thing("Interactables/JunctionBox")),
            new UseStep("Repair", b => !b.Prompt.Contains("Repair"), 5),
        };
    }
}

/// <summary>A robo taxi called at the stand, ridden to the drop-off.</summary>
public sealed class RideTaxiActivity : StepsActivity
{
    public RideTaxiActivity()
        : base("ride a robo taxi", 1)
    {
    }

    public override string Zone
    {
        get { return ZoneIds.Town; }
    }

    public override double UsualSeconds
    {
        get { return 90; }
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep>
        {
            new WalkToStep("walk to the taxi stand", b => b.Thing("Interactables/TaxiStand")),
            new UseStep("robo taxi", b => b.ZoneId.StartsWith(ZoneIds.Taxi), 10, true),
            new DoStep("ride to the drop-off", 180, (b, d) => b.ZoneId == ZoneIds.Town ? StepResult.Done : StepResult.Running, true),
        };
    }
}

/// <summary>The subway: a tag on the wall (once; after that the wall refuses), the visitor book read.</summary>
public sealed class TagSubwayActivity : StepsActivity
{
    public TagSubwayActivity()
        : base("tag the subway", 2)
    {
    }

    public override string Zone
    {
        get { return ZoneIds.Subway; }
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep>
        {
            new WalkToStep("walk to the wall", b => b.Thing("Interactables/SubwayWall")),
            new UseStep("Spray", b => false, 4, false, true),
            new WalkToStep("walk to the visitor book", b => b.Thing("Interactables/VisitorBook")),
            new UseStep("visitor book", b => b.IsOpen<MmoGame3d.Ui.VisitorBookPanel>()),
            new PauseStep(1.5),
            VisitorBookUi.TurnPage(),
            new PauseStep(1.5),
            new CloseAllStep(),
        };
    }
}

/// <summary>The outskirts: a wander, and the old hardware chest opened.</summary>
public sealed class OutskirtsActivity : StepsActivity
{
    public OutskirtsActivity()
        : base("go to the outskirts", 2)
    {
    }

    public override string Zone
    {
        get { return ZoneIds.Outskirts; }
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep>
        {
            new WanderStep(6 + (body.Random.NextDouble() * 8)),
            new WalkToStep("walk to the chest", b => b.Thing("Interactables/OldHardwareChest")),
            new UseStep("Open the", b => !b.Prompt.Contains("Open the"), 4),
        };
    }
}

/// <summary>The meadows: a long wander over the hills.</summary>
public sealed class WalkMeadowsActivity : StepsActivity
{
    public WalkMeadowsActivity()
        : base("walk the meadows", 2)
    {
    }

    public override string Zone
    {
        get { return ZoneIds.Meadows; }
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep> { new WanderStep(20 + (body.Random.NextDouble() * 25)) };
    }
}

/// <summary>
/// Back to Old Town from anywhere else: chosen often when away, since most things start
/// there, and the driver's way home when one thing is all it may do and that cannot start.
/// </summary>
public sealed class BackToTownActivity : TravelActivity
{
    public BackToTownActivity()
        : base(ZoneIds.Town, "go back to town", 20)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return BotWhere.InWorld(body) && body.ZoneId != ZoneIds.Town;
    }
}

/// <summary>Out of a trap: eight directions in turn. The driver's, after two failed walks or a stuck judge.</summary>
public sealed class EscapeActivity : StepsActivity
{
    public EscapeActivity()
        : base("get unstuck", 0)
    {
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep> { new EscapeStep() };
    }
}
