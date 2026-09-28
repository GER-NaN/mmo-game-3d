namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Dev.Screens;

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
