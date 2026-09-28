namespace MmoGame3d.Dev;

using System.Collections.Generic;

/// <summary>
/// One thing bots do in the game, in one file under game/dev/bots/features/: its name,
/// how often free bots pick it, and its steps (a BotPlan). Every class that derives from
/// this is found when bots start (BotCatalog); nothing else needs changing.
///
///   In normal play:      free bots pick it now and then, by its weight.
///   Only this, again:    .\scripts\bot-try.ps1 "its name"
///
/// Override Zone for one that happens in one zone (the bot travels there first), and
/// CanStart for one that needs something where it is. See features/visit_zone/VisitZone.cs.
/// </summary>
public abstract class BotFeature : StepsActivity
{
    // name: what the bot says it is doing, and what bot-try.ps1 takes. weight: how often
    // free bots pick it among everything else (most are 1 to 4).
    protected BotFeature(string name, int weight)
        : base(name, weight)
    {
    }

    protected abstract BotPlan Steps(BotBody body);

    protected sealed override List<BotStep> Plan(BotBody body)
    {
        return Steps(body).Steps;
    }
}
