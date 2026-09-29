namespace MmoGame3d.Bots;

using System.Collections.Generic;
using MmoGame3d.Ui;

/// <summary>
/// Activities with the account's characters: leaving the world for the character screen
/// and coming back as another character, and making a new character there. The account
/// has a few slots; when none is free, making one stops as completed.
/// </summary>
public static class CharacterActivities
{
    // Names are not unique, and at most 16 characters.
    private static readonly string[] Names = { "Second Me", "Alt", "Robo Fan", "Night Owl", "Quinn" };

    public static List<BotActivity> All()
    {
        return new List<BotActivity>
        {
            SwitchCharacter(),
            CreateCharacter(),

            // The character screen's Back: the main menu, then in again by Play.
            new BotActivity("back-from-characters", plan => ToCharacterScreen(plan.InWorld())
                .Click("Back", body => BotScreens.FirstButton(body.Find<CharacterSelect>()!, "Back"))
                .WaitFor<MainMenu>()
                .InWorld()),
        };
    }

    // Back to the character screen, then Play on a character other than this one, seen by
    // the name in the world.
    private static BotActivity SwitchCharacter()
    {
        return new BotActivity("switch-character", plan =>
        {
            string before = "";

            return ToCharacterScreen(plan.InWorld().Do("note the name", body => before = body.Player!.DisplayName))
                .StopIf("there is no other character", body => BotScreens.PlayOtherThan(body.Find<CharacterSelect>(), before) == null)
                .Click("Play on another character", body => BotScreens.PlayOtherThan(body.Find<CharacterSelect>(), before))
                .InWorld()
                .Until("playing someone else", body => body.Player != null && body.Player.DisplayName != before, 10);
        }).Says("Let me try my other character.");
    }

    // Back to the character screen, a new character in a free slot (a random look, the
    // name given), then Play on it.
    private static BotActivity CreateCharacter()
    {
        return new BotActivity("create-character", plan => ToCharacterScreen(plan.InWorld())
            .StopIf("no slot is free", body => body.Find<CharacterSelect>() == null || BotScreens.FirstButton(body.Find<CharacterSelect>()!, "Create a character") == null)
            .Click("Create a character", body => BotScreens.FirstButton(body.Find<CharacterSelect>()!, "Create a character"))
            .UntilOpen("character-creator")
            .Click<CharacterCreator>("%Random")
            .Click<CharacterCreator>("%Name")
            .Step(new TypeStep("a name", body => Names[body.Random.Next(Names.Length)]))
            .Click<CharacterCreator>("%Done")
            .UntilClosed("character-creator")
            .WaitFor<CharacterSelect>()
            .Click("Play on the newest character", body => BotScreens.LastPlay(body.Find<CharacterSelect>()))
            .InWorld()
            .Until("playing under the new name", body => System.Array.IndexOf(Names, body.Player?.DisplayName ?? "") >= 0, 10))
            .Says("Time for a fresh start.");
    }

    // The game menu's Leave, then Play on the main menu: the character screen.
    private static BotPlan ToCharacterScreen(BotPlan plan)
    {
        return plan
            .Press("ui_cancel")
            .UntilOpen("game-menu")
            .Click<InGameMenu>("%Leave")
            .WaitFor<MainMenu>()
            .Click<MainMenu>("%Play")
            .WaitFor<CharacterSelect>();
    }
}
