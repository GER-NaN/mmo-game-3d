namespace MmoGame3d.Dev.Screens;

using MmoGame3d.Ui;

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
