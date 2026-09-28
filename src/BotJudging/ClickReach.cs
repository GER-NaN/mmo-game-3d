namespace MmoGame3d.BotJudging;

/// <summary>
/// Whether a person could click a button now, and if not what they would do: what can be
/// clicked is the window, and inside a list only the part the list shows. A button in
/// the window but clipped by its list is scrolled to, not clicked (a click there hits
/// what lies under it).
/// </summary>
public static class ClickReach
{
    public const int MaxWheels = 8;

    // scroll: the list the button is in, or null. wheeled: turns already spent on it.
    public static ClickMove Decide(ScreenRect window, ScreenRect? scroll, ScreenRect button, int wheeled)
    {
        if (!button.HasSize)
        {
            return ClickMove.Wait;
        }

        ScreenRect shown = scroll == null ? window : scroll.Intersection(window);

        if (shown.HasSize && shown.Encloses(button))
        {
            return ClickMove.Click;
        }

        if (scroll == null || !shown.HasSize || wheeled >= MaxWheels)
        {
            return ClickMove.OffScreen;
        }

        return button.Bottom > shown.Bottom ? ClickMove.WheelDown : ClickMove.WheelUp;
    }
}
