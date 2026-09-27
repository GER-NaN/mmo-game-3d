namespace MmoGame3d.Dev.Features;

using System.Collections.Generic;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Terminals;
using MmoGame3d.Ui;

/// <summary>
/// Whois in bot testing: a bot looks at its own page and edits it, the Plan and the
/// Show Skills setting, on the phone. The first feature file (IBotFeature); a model for
/// the next.
/// </summary>
public sealed class BotWhoisFeature : IBotFeature
{
    private static readonly string[] Plans =
    {
        "Fixing the street lights. Need RAM sticks.",
        "LFG substation repair",
        "Selling batteries cheap",
        "Just walking around",
        "Hunting drones tonight",
    };

    public void AddTo(BotCatalog catalog)
    {
        catalog.Add(new BotActivity("edit my Whois page", 2, body => body.Zone != null && !body.ZoneId.StartsWith("taxi") && (body.PhonePercent > 5 || body.Has(ItemType.Phone)), body =>
            new BotPlan()
                .Equip(ItemType.Phone)
                .Phone()
                .Click(TerminalScreen.AppGroupPrefix + TerminalApps.Whois)
                .Pause(1)
                .Click(TerminalScreen.WhoisMineGroup)
                .Pause(1)
                .Type(TerminalScreen.WhoisPlanGroup, Plans[body.Random.Next(Plans.Length)])
                .Pause(1)
                .Click(TerminalScreen.WhoisShowSkillsGroup, "", true)
                .Pause(1.5)
                .Close()
                .Steps));
    }
}
