namespace MmoGame3d.Dev.Screens;

using MmoGame3d.Ui;

/// <summary>The subway's visitor book (VisitorBookPanel).</summary>
public static class VisitorBookUi
{
    public static BotStep TurnPage()
    {
        return ScreenSteps.ClickAny("turn a page", VisitorBookPanel.NextGroup, true);
    }
}
