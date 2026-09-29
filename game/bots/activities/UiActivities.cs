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
            RemapAKey(),
            Wardrobe(),
            WardrobeByHand(),
            WardrobeCancelled(),
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

    // A panel on its own key: opened, looked at, closed with the same key. The map is only
    // where there is one (not indoors).
    private static BotActivity Panel(string name, string key)
    {
        return new BotActivity(name, plan => plan
            .InWorld()
            .StopIf("there is no map here", body => key == "map" && body.Zone != null && body.Zone.MapSize == Godot.Vector2.Zero)
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
                // Low, then high: a kept player's slider may already sit at either spot.
                .ClickAt<SettingsPanel>("%Sensitivity", 0.2f, 0.5f)
                .Wait(0.3)
                .Do("note the sensitivity", body => before = SensitivityText(body))
                .ClickAt<SettingsPanel>("%Sensitivity", 0.8f, 0.5f)
                .Until("the sensitivity changed", body => SensitivityText(body) != before)
                .Click<SettingsPanel>("%Back")
                .UntilClosed("settings")
                .Click<InGameMenu>("%Resume")
                .UntilClosed("game-menu");
        }).Says("Tweaking my settings.");
    }

    // Jump bound to J in the settings, a jump by J seen on the server's body, then the keys
    // reset and Space shown again. The execution's own settings file keeps the change.
    private static BotActivity RemapAKey()
    {
        return new BotActivity("remap-a-key", plan =>
        {
            float ground = 0;
            float highest = 0;

            return plan
                .InWorld()
                .Press("jump")
                .Wait(1)
                .Press("ui_cancel")
                .UntilOpen("game-menu")
                .Click<InGameMenu>("%Settings")
                .UntilOpen("settings")
                .Click("the Jump key", body => KeyButton(body, "jump"))
                .Until("the Jump key waits for a key", body => KeyButton(body, "jump")?.Text == "Press a key", 3)
                .Press(Godot.Key.J)
                .Until("Jump shows J", body => KeyButton(body, "jump")?.Text == "J", 3)
                .Click<SettingsPanel>("%Back")
                .UntilClosed("settings")
                .Click<InGameMenu>("%Resume")
                .UntilClosed("game-menu")
                .Wait(1)
                .Do("note the ground", body => { ground = body.Player!.NetPosition.Y; highest = ground; })
                .Press(Godot.Key.J)
                .Until("the server's body rose 0.5 m by J", body =>
                {
                    highest = Godot.Mathf.Max(highest, body.Player?.NetPosition.Y ?? highest);
                    return highest - ground >= 0.5f;
                }, 3)
                .Press("ui_cancel")
                .UntilOpen("game-menu")
                .Click<InGameMenu>("%Settings")
                .UntilOpen("settings")
                .Click<SettingsPanel>("%ResetKeys")
                .Until("Jump shows Space again", body => KeyButton(body, "jump")?.Text == "Space", 3)
                .Click<SettingsPanel>("%Back")
                .UntilClosed("settings")
                .Click<InGameMenu>("%Resume")
                .UntilClosed("game-menu");
        }).Says("Let me rebind that.");
    }

    // The wardrobe closes back to the game menu; Resume is back to play.
    private static BotStep BackToPlay()
    {
        return new ClickStep("Resume", body => body.Find<InGameMenu>()?.GetNodeOrNull<Godot.Control>("%Resume"));
    }

    private static Godot.Button? KeyButton(BotBody body, string action)
    {
        return BotScreens.Named<Godot.Button>(body.Find<SettingsPanel>(), "Key_" + action);
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
                .Step(BackToPlay())
                .Until("the new look on the body", body => body.Player != null && body.Player.Look != before, 5);
        }).Says("New outfit, what do you think?", "Time for a change.");
    }

    // Each colour stepped through by hand, both ways, then kept.
    private static BotActivity WardrobeByHand()
    {
        return new BotActivity("wardrobe-by-hand", plan => plan
            .InWorld()
            .Press("ui_cancel")
            .UntilOpen("game-menu")
            .Click<InGameMenu>("%Wardrobe")
            .UntilOpen("character-creator")
            .ClickEach("the colour arrows", body => body.Find<CharacterCreator>(), button => button.Text == "<" || button.Text == ">")
            .Click<CharacterCreator>("%Done")
            .UntilClosed("character-creator")
                .Step(BackToPlay()))
            .Says("Trying every colour.");
    }

    // A new look tried and thrown away: the body keeps the old one.
    private static BotActivity WardrobeCancelled()
    {
        return new BotActivity("wardrobe-cancelled", plan =>
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
                .Click<CharacterCreator>("%Cancel")
                .UntilClosed("character-creator")
                .Step(BackToPlay())
                .Wait(1)
                .Until("the old look kept", body => body.Player != null && body.Player.Look == before, 2);
        }).Says("Nah, I'll keep this.");
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
