namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;

/// <summary>Whatever is open, poked at: buttons clicked, fields typed into, panels opened.</summary>
public sealed class PokeAroundActivity : StepsActivity
{
    public PokeAroundActivity()
        : base("poke around", 8)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return BotWhere.InWorld(body);
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep> { new PokeStep(false, 25 + (body.Random.NextDouble() * 20)), new CloseAllStep() };
    }
}
