namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Rules.World;

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
