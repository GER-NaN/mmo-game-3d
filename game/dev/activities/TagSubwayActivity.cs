namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Dev.Screens;
using MmoGame3d.Rules.World;

/// <summary>The subway: a tag on the wall (once; after that the wall refuses), the visitor book read.</summary>
public sealed class TagSubwayActivity : StepsActivity
{
    public TagSubwayActivity()
        : base("tag the subway", 2)
    {
    }

    public override string Zone
    {
        get { return ZoneIds.Subway; }
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep>
        {
            new WalkToStep("walk to the wall", b => b.Thing("Interactables/SubwayWall")),
            new UseStep("Spray", b => false, 4, false, true),
            new WalkToStep("walk to the visitor book", b => b.Thing("Interactables/VisitorBook")),
            new UseStep("visitor book", b => b.IsOpen<MmoGame3d.Ui.VisitorBookPanel>()),
            new PauseStep(1.5),
            VisitorBookUi.TurnPage(),
            new PauseStep(1.5),
            new CloseAllStep(),
        };
    }
}
