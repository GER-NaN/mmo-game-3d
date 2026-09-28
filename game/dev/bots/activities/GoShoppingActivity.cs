namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Dev.Screens;
using MmoGame3d.Rules.World;
using MmoGame3d.Ui;

/// <summary>
/// A look round the shop: something bought from the keeper, then the workbench, a battery
/// out of a phone and one in, whichever.
/// </summary>
public sealed class GoShoppingActivity : StepsActivity
{
    public GoShoppingActivity()
        : base("go shopping", 3)
    {
    }

    public override string Zone
    {
        get { return ZoneIds.Shop; }
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new BotPlan()
            .WalkTo("Interactables/Shopkeeper")
            .Use("Talk to", b => b.Usable(ShopPanel.BuyGroup) != null)
            .Pause(1)
            .Step(ShopUi.BuyAny())
            .Pause(1)
            .Close()
            .WalkTo("Interactables/Workbench")
            .Use("workbench", b => b.IsOpen<WorkbenchPanel>())
            .Pause(0.8)
            .Step(WorkbenchUi.TakeBatteryOut())
            .Pause(0.8)
            .Step(WorkbenchUi.PutAnyIn())
            .Pause(0.8)
            .Close()
            .Steps;
    }
}
