namespace MmoGame3d.Dev.Screens;

using MmoGame3d.Ui;

/// <summary>The party panel: leaving it.</summary>
public static class PartyUi
{
    public static bool InParty(BotBody body)
    {
        return body.Usable(PartyPanel.LeaveGroup) != null;
    }

    public static BotStep Leave()
    {
        return ScreenSteps.ClickAny("click Leave party", PartyPanel.LeaveGroup, true);
    }
}
