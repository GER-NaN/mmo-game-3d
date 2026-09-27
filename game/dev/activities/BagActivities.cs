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

/// <summary>A look at the map, then closed.</summary>
public sealed class LookAtMapActivity : StepsActivity
{
    public LookAtMapActivity()
        : base("look at the map", 1)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return BotWhere.InWorld(body);
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep> { LookUi.OpenMap(), new PauseStep(3), new CloseAllStep() };
    }
}

/// <summary>A look at skills, then at friends.</summary>
public sealed class LookAtSkillsActivity : StepsActivity
{
    public LookAtSkillsActivity()
        : base("look at skills and friends", 1)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return BotWhere.InWorld(body);
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep>
        {
            LookUi.OpenSkills(), new PauseStep(2.5), new CloseAllStep(),
            LookUi.OpenFriends(), new PauseStep(2.5), new CloseAllStep(),
        };
    }
}
