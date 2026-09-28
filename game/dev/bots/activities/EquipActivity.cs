namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.World;

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
