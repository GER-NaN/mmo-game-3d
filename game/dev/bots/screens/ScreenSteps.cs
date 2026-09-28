namespace MmoGame3d.Dev.Screens;

using System.Collections.Generic;
using Godot;

/// <summary>
/// The clicks the screen classes share. A button is found by its group, as a person finds
/// it by looking, and clicked through TryClick, so one outside what the window shows is
/// scrolled in first. Optional: done when there is none (nothing to click is an answer).
/// </summary>
public static class ScreenSteps
{
    // One of the group's buttons, picked at random once, so scrolling it into view does
    // not change the mind.
    public static BotStep ClickAny(string name, string group, bool optional = false)
    {
        Button? pick = null;
        return new DoStep(name, 4, (b, d) =>
        {
            if (pick == null || !GodotObject.IsInstanceValid(pick) || !pick.IsVisibleInTree())
            {
                List<Button> buttons = b.UsableAll(group);

                if (buttons.Count == 0)
                {
                    return optional ? StepResult.Done : StepResult.Running;
                }

                pick = buttons[b.Random.Next(buttons.Count)];
            }

            if (!b.TryClick(pick))
            {
                return StepResult.Running;
            }

            GD.Print("Bot: clicked " + pick.Text);
            return StepResult.Done;
        });
    }

    // The group's button on the row naming the item.
    public static BotStep ClickRow(string name, string group, string item, bool optional = false)
    {
        return new DoStep(name, 4, (b, d) =>
        {
            Button? button = b.RowButton(group, item);

            if (button == null)
            {
                return optional ? StepResult.Done : StepResult.Running;
            }

            if (!b.TryClick(button))
            {
                return StepResult.Running;
            }

            GD.Print("Bot: clicked " + button.Text + " (" + item + ")");
            return StepResult.Done;
        });
    }

    // The action's key, once (a panel's own key, the phone).
    public static BotStep Press(string name, string action)
    {
        return new DoStep(name, 1, (b, d) =>
        {
            BotBody.Press(action);
            return StepResult.Done;
        });
    }
}
