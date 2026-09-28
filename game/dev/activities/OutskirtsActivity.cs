namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Rules.World;

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
