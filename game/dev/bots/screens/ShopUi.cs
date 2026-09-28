namespace MmoGame3d.Dev.Screens;

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
