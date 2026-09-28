namespace MmoGame3d.Dev.Screens;

using MmoGame3d.Ui;

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

    // Out of the world, back to the main menu.
    public static BotStep Leave()
    {
        return ScreenSteps.ClickAny("leave to the main menu", InGameMenu.LeaveGroup);
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
