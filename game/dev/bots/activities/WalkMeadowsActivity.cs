namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Rules.World;

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
