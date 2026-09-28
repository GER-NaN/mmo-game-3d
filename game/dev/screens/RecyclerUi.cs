namespace MmoGame3d.Dev.Screens;

using MmoGame3d.Ui;

/// <summary>Old Town's recycler (RecyclerPanel).</summary>
public static class RecyclerUi
{
    public static BotStep RecycleOne()
    {
        return ScreenSteps.ClickAny("recycle one", RecyclerPanel.RecycleGroup, true);
    }
}
