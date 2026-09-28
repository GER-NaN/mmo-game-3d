namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Dev.Screens;

/// <summary>A stack dropped from a bag that holds something to sell.</summary>
public sealed class TidyBagActivity : StepsActivity
{
    public TidyBagActivity()
        : base("tidy the bag", 1)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return BotWhere.InWorld(body) && body.HasSomethingToSell;
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep> { BagUi.Open(), new PauseStep(1), BagUi.DropAny(), new PauseStep(1), new CloseAllStep() };
    }
}
