namespace MmoGame3d.Bots;

using System.Collections.Generic;
using MmoGame3d.Ui;

/// <summary>
/// Activities that work the game's screens and menus: the panels on keys, the game menu
/// and what it opens (settings, the wardrobe, leaving), the main menu's credits, emotes
/// through chat. Most check the screen opened and closed again; the ones that change
/// something check that it changed.
/// </summary>
public static class UiActivities
{
    public static List<BotActivity> All()
    {
        return new List<BotActivity>
        {
            Panel("look-at-bag", "inventory").Says("Let me check what I'm carrying.", "Bag's getting full."),
            Panel("look-at-map", "map").Says("Where am I again?", "The map helps."),
            Panel("look-at-skills", "skills").Says("How am I doing?"),
            Panel("look-at-friends", "social").Says("Who's around?"),

            new BotActivity("game-menu", plan => plan
                .InWorld()
                .Press("ui_cancel")
                .UntilOpen("game-menu")
                .Wait(1)
                .Click<InGameMenu>("%Resume")
                .UntilClosed("game-menu")),

            GameSettings(),
            Wardrobe(),
            LeaveAndReturn(),
            Emote("wave"),
            Emote("cheer"),
            Emote("sit"),

            new BotActivity("credits", plan => plan
                .WaitFor<MainMenu>()
                .Click<MainMenu>("%Credits")
                .WaitFor<CreditsPanel>()
                .Wait(1)
                .Click<CreditsPanel>("%Back")
                .Until("credits closed", body => body.Find<CreditsPanel>() == null)),
        };
    }

    // A panel on its own key: opened, looked at, closed with the same key.
    private static BotActivity Panel(string name, string key)
    {
        return new BotActivity(name, plan => plan
            .InWorld()
            .Press(key)
            .UntilOpen(key)
            .Wait(1.5)
            .Press(key)
            .UntilClosed(key));
    }

    // The game menu's settings: the mouse sensitivity slider moved by a click (each bot has
    // its own settings file, so it starts at the default), seen to move; then back out.
    private static BotActivity GameSettings()
    {
        return new BotActivity("game-settings", plan =>
        {
            string before = "";

            return plan
                .InWorld()
                .Press("ui_cancel")
                .UntilOpen("game-menu")
                .Click<InGameMenu>("%Settings")
                .UntilOpen("settings")
                .Do("note the sensitivity", body => before = SensitivityText(body))
                .ClickAt<SettingsPanel>("%Sensitivity", 0.8f, 0.5f)
                .Until("the sensitivity changed", body => SensitivityText(body) != before)
                .Click<SettingsPanel>("%Back")
                .UntilClosed("settings")
                .Click<InGameMenu>("%Resume")
                .UntilClosed("game-menu");
        }).Says("Tweaking my settings.");
    }

    // The wardrobe (the game menu's Wardrobe): a random new look, saved, and seen on the
    // player's body once the server has it.
    private static BotActivity Wardrobe()
    {
        return new BotActivity("wardrobe", plan =>
        {
            string before = "";

            return plan
                .InWorld()
                .Do("note the look", body => before = body.Player!.Look)
                .Press("ui_cancel")
                .UntilOpen("game-menu")
                .Click<InGameMenu>("%Wardrobe")
                .UntilOpen("character-creator")
                .Click<CharacterCreator>("%Random")
                .Wait(0.5)
                .Click<CharacterCreator>("%Random")
                .Click<CharacterCreator>("%Done")
                .UntilClosed("character-creator")
                .Until("the new look on the body", body => body.Player != null && body.Player.Look != before, 5);
        }).Says("New outfit, what do you think?", "Time for a change.");
    }

    // The game menu's Leave: back at the main menu, then into the world again through the
    // menus (the keeper presses Play, then Play on the character).
    private static BotActivity LeaveAndReturn()
    {
        return new BotActivity("leave-and-return", plan => plan
            .InWorld()
            .Press("ui_cancel")
            .UntilOpen("game-menu")
            .Click<InGameMenu>("%Leave")
            .UntilOpen("main-menu")
            .InWorld())
            .Says("Brb.", "Back in a moment.");
    }

    // An emote typed in chat, seen on the player's body.
    private static BotActivity Emote(string gesture)
    {
        return new BotActivity("emote-" + gesture, plan => plan
            .InWorld()
            .Press("chat")
            .Type("/" + gesture)
            .Until("the body " + gesture + "s", body => body.Player != null && body.Player.GestureId == gesture, 5));
    }

    private static string SensitivityText(BotBody body)
    {
        return body.Find<SettingsPanel>()?.GetNodeOrNull<Godot.Label>("%SensitivityLabel")?.Text ?? "";
    }
}
