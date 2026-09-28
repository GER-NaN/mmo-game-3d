namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;

/// <summary>Out of a trap: eight directions in turn. The driver's, after two failed walks or a stuck judge.</summary>
public sealed class EscapeActivity : StepsActivity
{
    public EscapeActivity()
        : base("get unstuck", 0)
    {
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep> { new EscapeStep() };
    }
}
