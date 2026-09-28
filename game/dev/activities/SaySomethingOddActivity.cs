namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Dev.Screens;

/// <summary>One of the hostile lines (markup, emoji, too long), in public chat.</summary>
public sealed class SaySomethingOddActivity : StepsActivity
{
    public SaySomethingOddActivity()
        : base("say something odd", 2)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return body.Zone != null;
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep> { ChatUi.Say(PokeStep.Lines[body.Random.Next(PokeStep.Lines.Length)]), new PauseStep(1.5) };
    }
}
