namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Dev.Screens;
using MmoGame3d.Rules.World;

/// <summary>Walking about Old Town: wandering, a word in chat, the EMP key pressed.</summary>
public sealed class WalkAroundTownActivity : StepsActivity
{
    public WalkAroundTownActivity()
        : base("walk around town", 6)
    {
    }

    public override string Zone
    {
        get { return ZoneIds.Town; }
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new BotPlan()
            .Wander(15 + (body.Random.NextDouble() * 30))
            .Step(ChatUi.SayInPassing(body.Random))
            .Wander(10 + (body.Random.NextDouble() * 20))
            .Step(ScreenSteps.Press("press R for the EMP", "emp"))
            .Steps;
    }
}
