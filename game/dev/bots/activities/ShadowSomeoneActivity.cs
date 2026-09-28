namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;

/// <summary>Another player followed at arm's length, through doors, using what they use.</summary>
public sealed class ShadowSomeoneActivity : StepsActivity
{
    public ShadowSomeoneActivity()
        : base("shadow someone", 8)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return BotWhere.InWorld(body);
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep> { new ShadowStep(40 + (body.Random.NextDouble() * 40)), new CloseAllStep() };
    }
}
