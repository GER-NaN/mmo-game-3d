namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Dev.Screens;

/// <summary>The bag opened, something put on at random, closed.</summary>
public sealed class CheckBagActivity : StepsActivity
{
    public CheckBagActivity()
        : base("check the bag", 2)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return BotWhere.InWorld(body);
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep> { BagUi.Open(), new PauseStep(1), BagUi.EquipAny(), new PauseStep(1.5), new CloseAllStep() };
    }
}
