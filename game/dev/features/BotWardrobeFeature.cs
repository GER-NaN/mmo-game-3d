namespace MmoGame3d.Dev.Features;

using System.Collections.Generic;
using MmoGame3d.Dev.Screens;

/// <summary>The wardrobe in bot testing (IBotFeature): the activity that changes a look.</summary>
public sealed class BotWardrobeFeature : IBotFeature
{
    public void AddTo(BotCatalog catalog)
    {
        catalog.Add(new ChangeLookActivity());
    }
}

/// <summary>
/// From the game menu, the wardrobe: a few steps along its rows, sometimes Random, then
/// saved, or now and then cancelled, which must leave the look as it was. Others see the
/// new look: the server syncs it, and every client redresses the body.
/// </summary>
public sealed class ChangeLookActivity : StepsActivity
{
    public ChangeLookActivity()
        : base("change my look", 1)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return BotWhere.InWorld(body);
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        BotPlan plan = new BotPlan()
            .Step(WardrobeUi.OpenMenu())
            .Pause(0.8)
            .Step(WardrobeUi.OpenWardrobe())
            .Pause(1);

        int changes = 2 + body.Random.Next(5);

        for (int i = 0; i < changes; i++)
        {
            plan = plan.Step(body.Random.Next(4) == 0 ? WardrobeUi.Randomize() : WardrobeUi.StepARow()).Pause(0.6);
        }

        return plan
            .Step(body.Random.Next(4) == 0 ? WardrobeUi.Cancel() : WardrobeUi.Save())
            .Pause(1)
            .Close()
            .Steps;
    }
}
