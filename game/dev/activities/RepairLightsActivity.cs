namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Rules.World;

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
