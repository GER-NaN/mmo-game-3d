namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Dev.Screens;
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
            .Step(ShopUi.Buy(name))
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
