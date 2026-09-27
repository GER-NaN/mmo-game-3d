namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Dev.Screens;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.World;

/// <summary>A public terminal (the library's or the street kiosk), online, a few apps, offline.</summary>
public sealed class UsePublicTerminalActivity : StepsActivity
{
    public UsePublicTerminalActivity()
        : base("use a public terminal", 4)
    {
    }

    public override string Zone
    {
        get { return ZoneIds.Town; }
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        // Chosen once, so it does not swing between the two on the way.
        string terminal = body.Random.Next(2) == 0 ? "Interactables/LibraryTerminal" : "Interactables/StreetKiosk";
        return new List<BotStep>
        {
            new WalkToStep("walk to a terminal", b => b.Thing(terminal)),
            TerminalUi.GoOnline(),
            TerminalUi.Browse(),
        };
    }
}

/// <summary>The phone out, a few apps, offline: wherever the bot is.</summary>
public sealed class UsePhoneActivity : StepsActivity
{
    public UsePhoneActivity()
        : base("use the phone", 3)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return body.Me != null;
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep> { TerminalUi.PhoneOut(), TerminalUi.Browse() };
    }
}

/// <summary>
/// A run of Agent Defense, on either public terminal, or on the phone when it has one with
/// charge: every way in to the game gets played.
/// </summary>
public sealed class PlayDefenseActivity : StepsActivity
{
    public PlayDefenseActivity()
        : base("play Agent Defense", 3)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return BotWhere.InWorld(body);
    }

    public override double UsualSeconds
    {
        get { return 90; }
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        bool phone = body.Has(ItemType.Phone) && body.PhonePercent > 20;
        int where = body.Random.Next(phone ? 3 : 2);
        List<BotStep> steps;

        if (where == 2)
        {
            steps = new BotPlan().Equip(ItemType.Phone).Steps;
            steps.Add(TerminalUi.PhoneOut());
        }
        else
        {
            // Not the phone: a public terminal, in town.
            string terminal = where == 0 ? "Interactables/LibraryTerminal" : "Interactables/StreetKiosk";
            steps = new List<BotStep>
            {
                new TravelStep(ZoneIds.Town),
                new WalkToStep("walk to a terminal", b => b.Thing(terminal)),
                TerminalUi.GoOnline(),
            };
        }

        steps.AddRange(TerminalUi.PlayDefense());
        steps.Add(new CloseAllStep());
        return steps;
    }
}

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
