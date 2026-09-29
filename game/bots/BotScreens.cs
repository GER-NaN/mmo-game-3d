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
    // The key that closes a screen, by its name in ClientView.OpenScreens; null for those
    // before the world (menus, character screens), which are not closed but played through.
    public static string? CloseKey(string screen)
    {
        switch (screen)
        {
            case "inventory":
            case "map":
            case "social":
            case "skills":
                return screen;
            case "game-menu":
            case "terminal":
            case "shop":
            case "workbench":
            case "give":
            case "recycler":
            case "college":
            case "garden":
            case "plant-card":
            case "visitor-book":
                return "ui_cancel";
            default:
                return null;
        }
    }

    // The Play button on the character screen's first character card. The cards are
    // built in code and their buttons have no names, so it goes by the button's text.
    public static Button? FirstPlay(CharacterSelect select)
    {
        return FirstButton(select, "Play");
    }

    // The first button with this text under a screen, in tree order.
    public static Button? FirstButton(Node under, string text)
    {
        foreach (Node child in under.GetChildren())
        {
            Button? button = child as Button;

            if (button != null && button.Text == text && button.IsVisibleInTree())
            {
                return button;
            }

            Button? inside = FirstButton(child, text);

            if (inside != null)
            {
                return inside;
            }
        }

        return null;
    }

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
