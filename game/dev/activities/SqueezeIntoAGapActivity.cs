namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;

/// <summary>Into the gap between two buildings, pushing: where players get wedged.</summary>
public sealed class SqueezeIntoAGapActivity : StepsActivity
{
    public SqueezeIntoAGapActivity()
        : base("squeeze into a gap", 8)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return body.Zone?.GetNodeOrNull("Buildings") != null;
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep> { new SqueezeStep() };
    }
}
