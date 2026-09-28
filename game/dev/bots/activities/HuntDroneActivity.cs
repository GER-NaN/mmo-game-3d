namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.World;

/// <summary>
/// A drone hunted down: after it on foot, under it, the EMP fired until it falls. Only
/// with an EMP emitter worn, and drones flying (the "fight drones" goal brings the EMP).
/// </summary>
public sealed class HuntDroneActivity : StepsActivity
{
    private static readonly List<BotFact> NeedsList = new List<BotFact> { BotFact.Wears(ItemType.EmpEmitter) };

    public HuntDroneActivity()
        : base("hunt a drone", 0)
    {
    }

    public override string Zone
    {
        get { return ZoneIds.Town; }
    }

    public override IReadOnlyList<BotFact> Needs
    {
        get { return NeedsList; }
    }

    public override double UsualSeconds
    {
        get { return 60; }
    }

    public override bool CanStart(BotBody body)
    {
        return !BotWhere.InTown(body) || body.LiveDrones().Count > 0;
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep> { new HuntStep() };
    }
}
