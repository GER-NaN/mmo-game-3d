namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;

/// <summary>Straight for a point past the zone's edge, jumping: the ways out of the world.</summary>
public sealed class RunForTheEdgeActivity : StepsActivity
{
    public RunForTheEdgeActivity()
        : base("run for the edge", 8)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return BotWhere.InWorld(body);
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep> { new EdgeStep(45) };
    }
}
