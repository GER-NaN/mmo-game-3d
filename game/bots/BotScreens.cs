namespace MmoGame3d.Bots;

using Godot;
using MmoGame3d.Rules.Items;
using MmoGame3d.Ui;

/// <summary>
/// Where bots find the controls that a screen builds from data, which have no names of
/// their own yet (bots.md T2). Kept here, one place, so a change to a screen changes
/// one method.
/// </summary>
public static class BotScreens
{
    // An app's button in a terminal's list, by the app's name ("Town cameras"). A locked
    // app's button reads "[locked] ..."; its tooltip is always the plain name.
    public static Button? TerminalApp(TerminalScreen? terminal, string appName)
    {
        if (terminal == null)
        {
            return null;
        }

        foreach (Node child in terminal.GetNode("%Apps").GetChildren())
        {
            Button? button = child as Button;

            if (button != null && button.TooltipText == appName)
            {
                return button;
            }
        }

        return null;
    }

    // The Equip button on the bag's row for an item of this type, found by the start of
    // the row's label ("Phone  battery 43%").
    public static Button? EquipButton(InventoryPanel? inventory, ItemType type)
    {
        if (inventory == null)
        {
            return null;
        }

        string name = ItemCatalog.Get(type).Name;

        foreach (Node row in inventory.GetNode("%Things").GetChildren())
        {
            HBoxContainer? box = row as HBoxContainer;

            if (box == null || box.IsQueuedForDeletion() || box.GetChildCount() < 2)
            {
                continue;
            }

            Label? label = box.GetChild(0) as Label;
            Button? button = box.GetChild(box.GetChildCount() - 1) as Button;

            if (label != null && button != null && label.Text.StartsWith(name) && button.Text == "Equip")
            {
                return button;
            }
        }

        return null;
    }
}
