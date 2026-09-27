namespace MmoGame3d.Dev.Features;

using System.Collections.Generic;
using MmoGame3d.Dev.Screens;
using MmoGame3d.Rules.Items;

/// <summary>
/// Whois in bot testing, in one file: the activity, registered with the catalog by the
/// feature (IBotFeature). A model for a feature's bot part.
/// </summary>
public sealed class BotWhoisFeature : IBotFeature
{
    public void AddTo(BotCatalog catalog)
    {
        catalog.Add(new EditWhoisActivity());
    }
}

/// <summary>The bot's own Whois page, on the phone: its plan rewritten, Show Skills flipped.</summary>
public sealed class EditWhoisActivity : StepsActivity
{
    private static readonly string[] Plans =
    {
        "Fixing the street lights. Need RAM sticks.",
        "LFG substation repair",
        "Selling batteries cheap",
        "Just walking around",
        "Hunting drones tonight",
    };

    public EditWhoisActivity()
        : base("edit my Whois page", 2)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return BotWhere.InWorld(body) && (body.PhonePercent > 5 || body.Has(ItemType.Phone));
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        List<BotStep> steps = new BotPlan().Equip(ItemType.Phone).Steps;
        steps.Add(TerminalUi.PhoneOut());
        steps.AddRange(TerminalUi.EditWhois(Plans[body.Random.Next(Plans.Length)]));
        steps.Add(new PauseStep(1.5));
        steps.Add(new CloseAllStep());
        return steps;
    }
}
