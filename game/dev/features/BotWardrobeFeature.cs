namespace MmoGame3d.Dev.Features;

using Godot;
using MmoGame3d.Ui;

/// <summary>
/// The wardrobe in bot testing: from the game menu, a bot changes its look (a few steps
/// through the rows, sometimes Random) and saves it, or thinks better of it and cancels.
/// Others see the new look: the server syncs it, and every client redresses the body.
/// </summary>
public sealed class BotWardrobeFeature : IBotFeature
{
    public void AddTo(BotCatalog catalog)
    {
        catalog.Add(new BotActivity("change my look", 1, body => body.Zone != null && !body.ZoneId.StartsWith("taxi"), body =>
        {
            BotPlan plan = new BotPlan()
                .Press("ui_cancel")
                .Pause(0.8)
                .Click(InGameMenu.WardrobeGroup)
                .Pause(1);

            // A few changes: a step along some row, or everything at once.
            int changes = 2 + body.Random.Next(5);

            for (int i = 0; i < changes; i++)
            {
                plan = body.Random.Next(4) == 0 ? plan.Click(CharacterCreator.RandomGroup) : plan.Do("step a row", 2, (b, d) => StepARow(b));
                plan = plan.Pause(0.6);
            }

            // Mostly saved; now and then cancelled, which must leave the look as it was.
            plan = body.Random.Next(4) == 0 ? plan.Click(CharacterCreator.CancelGroup) : plan.Click(CharacterCreator.DoneGroup);
            return plan.Pause(1).Close().Steps;
        }));
    }

    private static StepResult StepARow(BotBody body)
    {
        System.Collections.Generic.List<Button> buttons = body.UsableAll(CharacterCreator.StepGroup);

        if (buttons.Count == 0)
        {
            return StepResult.Running;
        }

        Button pick = buttons[body.Random.Next(buttons.Count)];
        body.Click(pick);
        return StepResult.Done;
    }
}
