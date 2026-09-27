namespace MmoGame3d.Dev.Screens;

using Godot;
using MmoGame3d.Ui;

/// <summary>The shop's keeper (ShopPanel): what is for sale.</summary>
public static class ShopUi
{
    public static BotStep BuyAny()
    {
        return ScreenSteps.ClickAny("buy something", ShopPanel.BuyGroup);
    }

    public static BotStep Buy(string itemName)
    {
        return ScreenSteps.ClickRow("buy " + itemName, ShopPanel.BuyGroup, itemName);
    }
}

/// <summary>The shop's workbench (WorkbenchPanel): a phone's battery out, and one in.</summary>
public static class WorkbenchUi
{
    public static BotStep TakeBatteryOut()
    {
        return ScreenSteps.ClickAny("take the battery out", WorkbenchPanel.RemoveGroup, true);
    }

    // The list puts the fullest first. After a battery comes out the bench asks the
    // server again, so its buttons come a moment later: waited for, then none means
    // there is nothing to put in.
    public static BotStep PutFullestIn()
    {
        double waited = 0;
        return new DoStep("put the fullest battery in", 5, (b, d) =>
        {
            Button? first = b.Usable(WorkbenchPanel.InsertGroup);

            if (first == null)
            {
                // The swap starts only with a spare battery, so no button is a failure,
                // not a swap done.
                waited += d;
                return waited < ButtonsWithin ? StepResult.Running : StepResult.Failed;
            }

            return b.TryClick(first) ? StepResult.Done : StepResult.Running;
        });
    }

    private const double ButtonsWithin = 3;

    public static BotStep PutAnyIn()
    {
        return ScreenSteps.ClickAny("put a battery in", WorkbenchPanel.InsertGroup, true);
    }
}

/// <summary>Old Town's recycler (RecyclerPanel).</summary>
public static class RecyclerUi
{
    public static BotStep RecycleOne()
    {
        return ScreenSteps.ClickAny("recycle one", RecyclerPanel.RecycleGroup, true);
    }
}

/// <summary>
/// The college's people (CollegePanel): the registrar's careers and Class, the professor's
/// rank.
/// </summary>
public static class CollegeUi
{
    // Whatever there is to do: the Class, a career, a rank.
    public static BotStep DoWhatIsThere()
    {
        double read = 0;
        return new DoStep("work the college panel", 6, (b, d) =>
        {
            read += d;

            if (read < 1)
            {
                return StepResult.Running;
            }

            Button? button = b.Usable(CollegePanel.ClassGroup) ?? b.Usable(CollegePanel.EnrollGroup) ?? b.Usable(CollegePanel.RankUpGroup);

            if (button == null)
            {
                return StepResult.Done;
            }

            return b.TryClick(button) ? StepResult.Done : StepResult.Running;
        });
    }
}

/// <summary>The bag (InventoryPanel): opened with its key, equip and drop on each row.</summary>
public static class BagUi
{
    public static BotStep Open()
    {
        return ScreenSteps.Press("open the bag", "inventory");
    }

    public static BotStep EquipAny()
    {
        return ScreenSteps.ClickAny("equip something", InventoryPanel.EquipGroup, true);
    }

    public static BotStep DropAny()
    {
        return ScreenSteps.ClickAny("drop a stack", InventoryPanel.DropGroup, true);
    }
}

/// <summary>The panels opened by their own keys: the map, skills, friends.</summary>
public static class LookUi
{
    public static BotStep OpenMap()
    {
        return ScreenSteps.Press("open the map", "map");
    }

    public static BotStep OpenSkills()
    {
        return ScreenSteps.Press("open skills", "skills");
    }

    public static BotStep OpenFriends()
    {
        return ScreenSteps.Press("open friends", "social");
    }
}

/// <summary>The party panel: leaving it.</summary>
public static class PartyUi
{
    public static bool InParty(BotBody body)
    {
        return body.Usable(PartyPanel.LeaveGroup) != null;
    }

    public static BotStep Leave()
    {
        return ScreenSteps.ClickAny("click Leave party", PartyPanel.LeaveGroup, true);
    }
}

/// <summary>The subway's visitor book (VisitorBookPanel).</summary>
public static class VisitorBookUi
{
    public static BotStep TurnPage()
    {
        return ScreenSteps.ClickAny("turn a page", VisitorBookPanel.NextGroup, true);
    }
}

/// <summary>
/// The game menu (Esc, InGameMenu) and the wardrobe it opens (CharacterCreator): a row
/// stepped, Random, Save or Cancel.
/// </summary>
public static class WardrobeUi
{
    public static BotStep OpenMenu()
    {
        return ScreenSteps.Press("open the game menu", "ui_cancel");
    }

    public static BotStep OpenWardrobe()
    {
        return ScreenSteps.ClickAny("open the wardrobe", InGameMenu.WardrobeGroup);
    }

    public static BotStep StepARow()
    {
        return ScreenSteps.ClickAny("step a row", CharacterCreator.StepGroup);
    }

    public static BotStep Randomize()
    {
        return ScreenSteps.ClickAny("random look", CharacterCreator.RandomGroup);
    }

    public static BotStep Save()
    {
        return ScreenSteps.ClickAny("save the look", CharacterCreator.DoneGroup);
    }

    public static BotStep Cancel()
    {
        return ScreenSteps.ClickAny("cancel the look", CharacterCreator.CancelGroup);
    }
}
