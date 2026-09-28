namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Rules.World;

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
