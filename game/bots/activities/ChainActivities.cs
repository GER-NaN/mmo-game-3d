namespace MmoGame3d.Bots;

using System.Collections.Generic;

/// <summary>
/// Chains: activities one after another, for orders random picks would almost never reach
/// (bots.md R4, the old related chains), each around one piece of state: money spent and
/// earned back, careers, two plants in a row, a phone taken to its limits, a party in a
/// taxi.
/// </summary>
public static class ChainActivities
{
    public static List<BotActivity> All()
    {
        return new List<BotActivity>
        {
            new BotActivity("buy-recycle-buy", plan => plan
                .Then("buy-battery")
                .Then("recycle")
                .Then("buy-battery")),

            // Whoever said yes rides along in the cabin.
            new BotActivity("party-ride", plan => plan
                .Then("invite-someone")
                .Then("ride-taxi")),

            new BotActivity("two-plants", plan => plan
                .Then("house-plant")
                .Then("house-plant")),

            new BotActivity("phone-day", plan => plan
                .Then("equip-phone")
                .Then("check-world-events")
                .Then("swap-battery")
                .Then("phone-terminal")),

            new BotActivity("new-character-first-steps", plan => plan
                .Then("create-character")
                .Then("look-at-bag")
                .Then("equip-phone")
                .Then("wander-town")),
        };
    }
}
