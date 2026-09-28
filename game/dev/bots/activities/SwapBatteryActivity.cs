namespace MmoGame3d.Dev.Activities;

using System;
using System.Collections.Generic;
using MmoGame3d.Dev.Screens;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.World;
using MmoGame3d.Ui;

/// <summary>
/// At the shop's workbench: the phone's battery out, and the fullest spare in. Only worth
/// the trip with a spare clearly fuller than the phone's.
/// </summary>
public sealed class SwapBatteryActivity : StepsActivity
{
    private static readonly List<BotFact> NeedsList = new List<BotFact> { BotFact.Has(ItemType.Battery) };
    private static readonly List<BotFact> GivesList = new List<BotFact> { BotFact.PhoneAtLeast(50) };

    public SwapBatteryActivity()
        : base("swap the battery", 0)
    {
    }

    public override string Zone
    {
        get { return ZoneIds.Shop; }
    }

    public override IReadOnlyList<BotFact> Needs
    {
        get { return NeedsList; }
    }

    public override IReadOnlyList<BotFact> Gives
    {
        get { return GivesList; }
    }

    public override bool CanStart(BotBody body)
    {
        return body.PhonePercent >= 0 && body.SpareBatteryPercent >= Math.Max(50, body.PhonePercent + 30);
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new BotPlan()
            .WalkTo("Interactables/Workbench")
            .Use("workbench", b => b.IsOpen<WorkbenchPanel>())
            .Pause(0.8)
            .Step(WorkbenchUi.TakeBatteryOut())
            .Pause(0.8)
            .Step(WorkbenchUi.PutFullestIn())
            .Pause(0.8)
            .Close()
            .Steps;
    }
}
