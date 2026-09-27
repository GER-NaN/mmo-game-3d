namespace MmoGame3d.Dev.Activities;

using System;
using System.Collections.Generic;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Shops;
using MmoGame3d.Rules.World;
using MmoGame3d.Ui;

/// <summary>Buying one thing from the electronics shop's keeper.</summary>
public sealed class BuyActivity : StepsActivity
{
    private readonly ItemType _type;
    private readonly List<BotFact> _needs;
    private readonly List<BotFact> _gives;

    public BuyActivity(ItemType type)
        : base("buy " + ItemCatalog.Get(type).Name, 0)
    {
        _type = type;
        _needs = new List<BotFact> { BotFact.MoneyAtLeast(Price(type)) };
        _gives = new List<BotFact> { BotFact.Has(type) };
    }

    public override string Zone
    {
        get { return ZoneIds.Shop; }
    }

    public override IReadOnlyList<BotFact> Needs
    {
        get { return _needs; }
    }

    public override IReadOnlyList<BotFact> Gives
    {
        get { return _gives; }
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        string name = ItemCatalog.Get(_type).Name;
        return new BotPlan()
            .WalkTo("Interactables/Shopkeeper")
            .Use("Talk to", b => b.Usable(ShopPanel.BuyGroup) != null)
            .Pause(1)
            .Click(ShopPanel.BuyGroup, name)
            .Pause(1.5)
            .Close()
            .Steps;
    }

    private static int Price(ItemType type)
    {
        foreach (ShopOffer offer in Shops.Offers(Shops.Electronics))
        {
            if (offer.Type == type)
            {
                return offer.Price;
            }
        }

        return int.MaxValue;
    }
}

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
            .Click(WorkbenchPanel.RemoveGroup, "", true)
            .Pause(0.8)
            .Click(WorkbenchPanel.InsertGroup, "", true)
            .Pause(0.8)
            .Close()
            .Steps;
    }
}

/// <summary>Old Town's recycler: one thing from the bag, for money.</summary>
public sealed class RecycleActivity : StepsActivity
{
    private static readonly List<BotFact> NeedsList = new List<BotFact> { BotFact.HasSomethingToSell() };
    private static readonly List<BotFact> GivesList = new List<BotFact> { BotFact.MoneyAtLeast(0) };

    public RecycleActivity()
        : base("recycle for money", 0)
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

    public override IReadOnlyList<BotFact> Gives
    {
        get { return GivesList; }
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new BotPlan()
            .WalkTo("Interactables/Recycler")
            .Use("recycler", b => b.IsOpen<RecyclerPanel>())
            .Pause(1)
            .Click(RecyclerPanel.RecycleGroup)
            .Pause(1)
            .Close()
            .Steps;
    }
}

/// <summary>Whatever lies on the ground nearby, up to three things.</summary>
public sealed class PickUpActivity : StepsActivity
{
    private static readonly List<BotFact> GivesList = new List<BotFact> { BotFact.HasSomethingToSell() };

    public PickUpActivity()
        : base("pick up things", 0)
    {
    }

    public override IReadOnlyList<BotFact> Gives
    {
        get { return GivesList; }
    }

    public override bool CanStart(BotBody body)
    {
        return body.Zone != null && !ZoneIds.IsInstance(body.ZoneId) && body.GroundItems().Count > 0;
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep> { new PickUpStep() };
    }
}

/// <summary>Putting on something from the bag: the phone, the EMP emitter.</summary>
public sealed class EquipActivity : StepsActivity
{
    private readonly ItemType _type;
    private readonly List<BotFact> _needs;
    private readonly List<BotFact> _gives;

    public EquipActivity(ItemType type)
        : base("equip " + ItemCatalog.Get(type).Name, 0)
    {
        _type = type;
        _needs = new List<BotFact> { BotFact.Has(type) };
        _gives = new List<BotFact> { BotFact.Wears(type) };
    }

    public override IReadOnlyList<BotFact> Needs
    {
        get { return _needs; }
    }

    public override IReadOnlyList<BotFact> Gives
    {
        get { return _gives; }
    }

    public override bool CanStart(BotBody body)
    {
        return body.Zone != null && !ZoneIds.IsInstance(body.ZoneId);
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new BotPlan().Equip(_type).Close().Steps;
    }
}
