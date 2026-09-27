namespace MmoGame3d.Dev;

using Godot;
using MmoGame3d.Ui;

/// <summary>
/// Keeps a bot (--bot) in the game for a long run. It lives as long as the client, so it
/// sees the screens the bot itself does not: back at the main menu (a lost connection, a
/// refused login, a stray click on Leave) it waits for the server to let the old session
/// go, then clicks Play; the game menu left open too long, it clicks Resume. It acts
/// through clicks, like the bot.
/// </summary>
public partial class BotKeeper : Node
{
    // Long enough for the server to drop the old session, or the login is refused again.
    private const double MenuWait = 15;

    // Longer than the bot's own look at the settings.
    private const double GameMenuWait = 10;

    private double _onMenuFor;
    private double _onGameMenuFor;

    public override void _Process(double delta)
    {
        Button? play = GetTree().GetFirstNodeInGroup(MainMenu.PlayGroup) as Button;

        if (play != null && play.IsVisibleInTree() && !play.Disabled)
        {
            _onMenuFor += delta;

            if (_onMenuFor >= MenuWait)
            {
                _onMenuFor = 0;
                GD.Print("Bot: back at the main menu; clicking Play");
                BotDriver.Click(play.GetGlobalRect().GetCenter());
            }
        }
        else
        {
            _onMenuFor = 0;
        }

        Button? resume = GetTree().GetFirstNodeInGroup(InGameMenu.ResumeGroup) as Button;

        if (resume != null && resume.IsVisibleInTree())
        {
            _onGameMenuFor += delta;

            if (_onGameMenuFor >= GameMenuWait)
            {
                _onGameMenuFor = 0;
                GD.Print("Bot: the game menu is still open; clicking Resume");
                BotDriver.Click(resume.GetGlobalRect().GetCenter());
            }
        }
        else
        {
            _onGameMenuFor = 0;
        }
    }
}
