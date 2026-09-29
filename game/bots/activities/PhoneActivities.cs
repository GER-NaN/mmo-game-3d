namespace MmoGame3d.Bots;

using System.Collections.Generic;
using MmoGame3d.Rules.Items;
using MmoGame3d.Ui;

/// <summary>
/// Activities with the phone: wearing it, and going online with it.
/// </summary>
public static class PhoneActivities
{
    public static List<BotActivity> All()
    {
        return new List<BotActivity>
        {
            new BotActivity("equip-phone", new[] { BotFacts.PhoneEquipped }, plan => plan
                .InWorld()
                .StopIf("the phone is on already", BotFacts.PhoneEquipped.Holds)
                .Wait(1)
                .Press("inventory")
                .WaitFor<InventoryPanel>()
                .Click("Equip on the phone", body => BotScreens.RowButton(body.Find<InventoryPanel>()?.GetNode("%Things"), ItemType.Phone + "_", "Equip"))
                .Until(BotFacts.PhoneEquipped)
                .Press("inventory"))
                .Says("Where did I put my phone?", "Phone's on."),

            new BotActivity("phone-terminal", plan => plan
                .InWorld()
                .Wait(1)
                .Need(BotFacts.PhoneEquipped)
                .Press("phone")
                .WaitFor<TerminalScreen>())
                .Says("Checking my phone.", "Anyone else online?"),
        };
    }
}
