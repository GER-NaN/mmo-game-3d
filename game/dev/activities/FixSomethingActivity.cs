namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Town;

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
