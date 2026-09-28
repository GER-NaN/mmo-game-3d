namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Rules.World;
using MmoGame3d.Town;

/// <summary>The nearest person out walking in town, caught up with and talked to.</summary>
public sealed class TalkToTownspersonActivity : StepsActivity
{
    public TalkToTownspersonActivity()
        : base("talk to someone in town", 2)
    {
    }

    public override string Zone
    {
        get { return ZoneIds.Town; }
    }

    public override bool CanStart(BotBody body)
    {
        return BotWhere.InWorld(body) && Nearest(body) != null;
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep>
        {
            new WalkToStep("catch up with someone", Nearest),
            new UseStep("Talk to", b => false, 4, false, true),
        };
    }

    private static Node3D? Nearest(BotBody body)
    {
        Node? things = body.Zone?.GetNodeOrNull("Interactables");
        Townsperson? nearest = null;

        if (things == null)
        {
            return null;
        }

        foreach (Node node in things.GetChildren())
        {
            Townsperson? person = node as Townsperson;

            if (person != null && (nearest == null || body.DistanceTo(person.GlobalPosition) < body.DistanceTo(nearest.GlobalPosition)))
            {
                nearest = person;
            }
        }

        return nearest;
    }
}
