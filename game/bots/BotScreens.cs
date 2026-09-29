namespace MmoGame3d.Bots;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Client;
using MmoGame3d.Ui;

/// <summary>
/// Where bots find the controls that a screen builds from data, which have no names of
/// their own yet (bots.md T2). Kept here, one place, so a change to a screen changes
/// one method.
/// </summary>
public static class BotScreens
{
    // Not a screen: a text field with the keys, closed the same way.
    public const string TextField = "text-field";

    // Whether a screen is open, by its name in ClientView.OpenScreens.
    public static bool IsOpen(BotBody body, string screen)
    {
        ClientView? view = body.View;

        if (view == null)
        {
            return false;
        }

        foreach (string open in view.OpenScreens)
        {
            if (open == screen)
            {
                return true;
            }
        }

        return false;
    }

    // The key that closes a screen, by its name in ClientView.OpenScreens; null for those
    // before the world (menus, character screens), which are not closed but played through.
    public static string? CloseKey(string screen)
    {
        switch (screen)
        {
            case TextField:
                return "ui_cancel";
            case "inventory":
            case "map":
            case "social":
            case "skills":
                return screen;
            case "game-menu":
            case "settings":
            case "terminal":
            case "shop":
            case "workbench":
            case "give":
            case "recycler":
            case "college":
            case "garden":
            case "plant-card":
            case "character-creator":
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

    // The first visible button whose text starts so, under a screen, in tree order: for
    // buttons whose text carries a value ("Put in the loose battery at 43%").
    public static Button? FirstButtonStarting(Node? under, string start)
    {
        if (under == null)
        {
            return null;
        }

        foreach (Node child in under.GetChildren())
        {
            Button? button = child as Button;

            if (button != null && button.Text.StartsWith(start) && button.IsVisibleInTree() && !button.IsQueuedForDeletion())
            {
                return button;
            }

            Button? inside = FirstButtonStarting(child, start);

            if (inside != null)
            {
                return inside;
            }
        }

        return null;
    }

    // The first visible control of a type under a screen, in tree order: the text field
    // in a terminal's chat, say.
    public static T? FirstOf<T>(Node? under)
        where T : Control
    {
        if (under == null)
        {
            return null;
        }

        foreach (Node child in under.GetChildren())
        {
            T? found = child as T;

            if (found != null && found.IsVisibleInTree() && !found.IsQueuedForDeletion())
            {
                return found;
            }

            T? inside = FirstOf<T>(child);

            if (inside != null)
            {
                return inside;
            }
        }

        return null;
    }

    // Play on a character card whose name is not this one; null when every card is it.
    // A card is a column: the preview, the name, then Play at the bottom.
    public static Button? PlayOtherThan(Node? select, string name)
    {
        List<Button> plays = new List<Button>();
        AllButtons(select, "Play", plays);

        foreach (Button play in plays)
        {
            Label? cardName = play.GetParent()?.GetChildOrNull<Label>(1);

            if (cardName != null && cardName.Text != name)
            {
                return play;
            }
        }

        return null;
    }

    // Play on the last character card: the newest character.
    public static Button? LastPlay(Node? select)
    {
        List<Button> plays = new List<Button>();
        AllButtons(select, "Play", plays);
        return plays.Count > 0 ? plays[plays.Count - 1] : null;
    }

    private static void AllButtons(Node? under, string text, List<Button> found)
    {
        if (under == null)
        {
            return;
        }

        foreach (Node child in under.GetChildren())
        {
            Button? button = child as Button;

            if (button != null && button.Text == text && button.IsVisibleInTree() && !button.IsQueuedForDeletion())
            {
                found.Add(button);
            }

            AllButtons(child, text, found);
        }
    }

    // A node by its name anywhere under a screen, as this type (a control named for what
    // it holds, as "Take_street-lights"); null if none is visible.
    public static T? Named<T>(Node? under, string name)
        where T : Control
    {
        T? found = under?.FindChild(name, true, false) as T;
        return found != null && found.IsVisibleInTree() && !found.IsQueuedForDeletion() ? found : null;
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

    // How many visible buttons with this text are under a screen.
    public static int ButtonsWith(Node under, string text)
    {
        int count = 0;

        foreach (Node child in under.GetChildren())
        {
            Button? button = child as Button;

            if (button != null && button.Text == text && button.IsVisibleInTree() && !button.IsQueuedForDeletion())
            {
                count++;
            }

            count += ButtonsWith(child, text);
        }

        return count;
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

    // A button on a row of a list, by the start of the row's name and the button's name.
    // Rows are named for what they hold (bots.md T2): "Battery_Standard" in the shop,
    // "Phone_<id>" and "Stack_Battery_Standard" in the bag and the recycler.
    public static Button? RowButton(Node? list, string rowStart, string button)
    {
        if (list == null)
        {
            return null;
        }

        foreach (Node row in list.GetChildren())
        {
            if (row.IsQueuedForDeletion() || !row.Name.ToString().StartsWith(rowStart))
            {
                continue;
            }

            Button? found = row.GetNodeOrNull<Button>(button);

            if (found != null && found.IsVisibleInTree())
            {
                return found;
            }
        }

        return null;
    }
}
