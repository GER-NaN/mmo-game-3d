namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Dev.Screens;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.World;

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
