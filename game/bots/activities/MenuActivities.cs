namespace MmoGame3d.Bots;

using System.Collections.Generic;
using MmoGame3d.Ui;

/// <summary>
/// Activities before the world: the main menu and what it opens.
/// </summary>
public static class MenuActivities
{
    public static List<BotActivity> All()
    {
        return new List<BotActivity>
        {
            new BotActivity("main-menu", plan => plan
                .WaitFor<MainMenu>()),

            new BotActivity("quit", plan => plan
                .WaitFor<MainMenu>()
                .Click<MainMenu>("%Quit")
                .ExpectQuit()),

            new BotActivity("fullscreen", plan => plan
                .WaitFor<MainMenu>()
                .Click<MainMenu>("%Settings")
                .WaitFor<SettingsPanel>()
                .Click<SettingsPanel>("%Fullscreen")
                .Until(BotFacts.Fullscreen)
                .Click<SettingsPanel>("%Fullscreen")
                .Until("windowed", body => !BotFacts.Fullscreen.Holds(body))),
        };
    }
}
