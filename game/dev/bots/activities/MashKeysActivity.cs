namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;

/// <summary>The game's keys, fast and in any order.</summary>
public sealed class MashKeysActivity : StepsActivity
{
    public MashKeysActivity()
        : base("mash the keys", 10)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return body.Zone != null;
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep> { new MashStep(10 + (body.Random.NextDouble() * 15)), new CloseAllStep() };
    }
}
