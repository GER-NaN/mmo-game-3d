namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Dev.Screens;
using MmoGame3d.Rules.World;

/// <summary>
/// The library terminal's Town cameras: a drone spotted and reported, for the town's pay.
/// Money without spending any, when there is nothing to sell.
/// </summary>
public sealed class ReportDronesActivity : StepsActivity
{
    private static readonly List<BotFact> GivesList = new List<BotFact> { BotFact.MoneyAtLeast(0) };

    public ReportDronesActivity()
        : base("report drones on the cameras", 0)
    {
    }

    public override string Zone
    {
        get { return ZoneIds.Town; }
    }

    public override IReadOnlyList<BotFact> Gives
    {
        get { return GivesList; }
    }

    public override bool CanStart(BotBody body)
    {
        return body.LiveDrones().Count > 0 || !BotWhere.InTown(body);
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        List<BotStep> steps = new List<BotStep>
        {
            new WalkToStep("walk to the library terminal", b => b.Thing("Interactables/LibraryTerminal")),
            TerminalUi.GoOnline(),
        };
        steps.AddRange(TerminalUi.WatchCameras());
        steps.Add(new CloseAllStep());
        return steps;
    }
}
