namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// A plan of steps, written the way it reads:
///
///     new BotPlan()
///         .Door("ToShop")
///         .WalkTo("Interactables/Shopkeeper")
///         .Use("Talk to", b => b.Usable(ShopPanel.BuyGroup) != null)
///         .Click(ShopPanel.BuyGroup, "Battery")
///         .Close()
///         .Door("ToTown")
///         .Steps
///
/// Each call adds one step (BotSteps.cs), with its time limit and its way out.
/// </summary>
public sealed class BotPlan
{
    public List<BotStep> Steps { get; } = new List<BotStep>();

    public BotPlan Step(BotStep step)
    {
        Steps.Add(step);
        return this;
    }

    // To another zone, door by door from wherever the bot is.
    public BotPlan Travel(string zoneId)
    {
        return Step(new TravelStep(zoneId));
    }

    // Through a door of the current zone (by its node name under Doors).
    public BotPlan Door(string door)
    {
        return Step(new DoorStep(door));
    }

    // To a thing in the current zone (by its path under the zone).
    public BotPlan WalkTo(string path, float near = 1.6f)
    {
        return Step(new WalkToStep("walk to " + path.Substring(path.LastIndexOf('/') + 1), b => b.Thing(path), near));
    }

    // F at the thing in reach whose prompt holds this text, until the result shows.
    public BotPlan Use(string prompt, Func<BotBody, bool> until, double limit = 8)
    {
        return Step(new UseStep(prompt, until, limit));
    }

    // One tap of F, for a use whose result the bot cannot see.
    public BotPlan UseOnce(string prompt)
    {
        return Step(new UseStep(prompt, b => false, 4, false, true));
    }

    // A button of the group, on the row naming the item if one is given; optional: done
    // with none there.
    public BotPlan Click(string group, string item = "", bool optional = false)
    {
        return Step(new DoStep("click " + group + (item.Length > 0 ? " (" + item + ")" : ""), 4, (b, d) =>
        {
            Button? button = item.Length > 0 ? b.RowButton(group, item) : b.Usable(group);

            if (button == null)
            {
                return optional ? StepResult.Done : StepResult.Running;
            }

            if (!b.TryClick(button))
            {
                return StepResult.Running;
            }

            GD.Print("Bot: clicked " + button.Text + (item.Length > 0 ? " (" + item + ")" : ""));
            return StepResult.Done;
        }));
    }

    // A text field of the group: a click into it, the text, Enter.
    public BotPlan Type(string group, string text)
    {
        return Step(new DoStep("type into " + group, 4, (b, d) =>
        {
            LineEdit? field = b.Me?.GetTree().GetFirstNodeInGroup(group) as LineEdit;

            if (field == null || !field.IsVisibleInTree())
            {
                return StepResult.Running;
            }

            GD.Print("Bot: typing \"" + text + "\"");
            BotDriver.Click(field.GetGlobalRect().GetCenter());
            BotDriver.Type(text);
            return StepResult.Done;
        }));
    }

    // A key, as a press and a release.
    public BotPlan Press(string action)
    {
        return Step(new DoStep("press " + action, 1, (b, d) =>
        {
            BotBody.Press(action);
            return StepResult.Done;
        }));
    }

    // The item worn: done at once if it is; otherwise the bag, Equip on its row, the bag
    // shut. A new player's phone is in the bag, not worn.
    public BotPlan Equip(Rules.Items.ItemType type)
    {
        string name = Rules.Items.ItemCatalog.Get(type).Name;
        int stage = 0;
        double wait = 0;
        return Step(new DoStep("equip " + name, 8, (b, d) =>
        {
            if (b.Wears(type))
            {
                if (stage > 0 && b.IsOpen<Ui.InventoryPanel>())
                {
                    BotBody.Press("inventory");
                }

                return StepResult.Done;
            }

            wait -= d;

            if (wait > 0)
            {
                return StepResult.Running;
            }

            wait = 0.8;

            if (!b.IsOpen<Ui.InventoryPanel>())
            {
                stage = 1;
                BotBody.Press("inventory");
                return StepResult.Running;
            }

            Button? equip = b.RowButton(Ui.InventoryPanel.EquipGroup, name);

            if (equip == null)
            {
                return StepResult.Failed;
            }

            GD.Print("Bot: clicking Equip (" + name + ")");
            b.Click(equip);
            return StepResult.Running;
        }));
    }

    // Online on the phone: P, then the terminal within a few seconds, or it is dead.
    public BotPlan Phone()
    {
        bool pressed = false;
        return Step(new DoStep("take the phone out", 4, (b, d) =>
        {
            if (b.IsOnline)
            {
                return StepResult.Done;
            }

            if (!pressed)
            {
                pressed = true;
                b.Stop();
                BotBody.Press("phone");
            }

            return StepResult.Running;
        }));
    }

    public BotPlan Pause(double seconds)
    {
        return Step(new PauseStep(seconds));
    }

    public BotPlan Wander(double seconds)
    {
        return Step(new WanderStep(seconds));
    }

    // Everything open closed, offline included.
    public BotPlan Close()
    {
        return Step(new CloseAllStep());
    }

    // A line in public chat, or an emote ("/wave").
    public BotPlan Say(string line)
    {
        return Step(new DoStep("say \"" + line + "\"", 1, (b, d) =>
        {
            b.Chat(line);
            return StepResult.Done;
        }));
    }

    // A step written in place.
    public BotPlan Do(string name, double limit, Func<BotBody, double, StepResult> tick)
    {
        return Step(new DoStep(name, limit, tick));
    }
}
