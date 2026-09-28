namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Dev.Screens;
using MmoGame3d.Rules.World;
using MmoGame3d.Ui;

/// <summary>Old Town's recycler: one thing from the bag, for money.</summary>
public sealed class RecycleActivity : StepsActivity
{
    private static readonly List<BotFact> NeedsList = new List<BotFact> { BotFact.HasSomethingToSell() };
    private static readonly List<BotFact> GivesList = new List<BotFact> { BotFact.MoneyAtLeast(0) };

    public RecycleActivity()
        : base("recycle something", 1)
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
            .Step(RecyclerUi.RecycleOne())
            .Pause(1)
            .Close()
            .Steps;
    }
}
