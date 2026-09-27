namespace MmoGame3d.BotJudging;

/// <summary>A rectangle on the screen, in pixels: where, and how big.</summary>
public sealed class ScreenRect
{
    public ScreenRect(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public float X { get; }

    public float Y { get; }

    public float Width { get; }

    public float Height { get; }

    public float Right
    {
        get { return X + Width; }
    }

    public float Bottom
    {
        get { return Y + Height; }
    }

    public bool HasSize
    {
        get { return Width >= 1 && Height >= 1; }
    }

    public bool Encloses(ScreenRect other)
    {
        return other.X >= X && other.Y >= Y && other.Right <= Right && other.Bottom <= Bottom;
    }

    public ScreenRect Intersection(ScreenRect other)
    {
        float left = System.Math.Max(X, other.X);
        float top = System.Math.Max(Y, other.Y);
        float right = System.Math.Min(Right, other.Right);
        float bottom = System.Math.Min(Bottom, other.Bottom);
        return new ScreenRect(left, top, System.Math.Max(0, right - left), System.Math.Max(0, bottom - top));
    }
}

public enum ClickMove
{
    // Clickable where it is.
    Click,

    // Just made: no size or place until the next layout.
    Wait,

    // In a list, outside what the list shows: turn the wheel over the list.
    WheelDown,
    WheelUp,

    // Nothing brings it into view: a finding, never a click.
    OffScreen,
}

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

/// <summary>
/// A plant at the potting table, as its maker sees it: finished means the table said the
/// plant was made; a failed run with no piece ever on the soil means the drags did not
/// land. A cancelled run is not judged.
/// </summary>
public static class PlantCheck
{
    public static string? Judge(bool finished, bool cancelled, bool madeSeen, int mostPieces, bool piecesOffered, string status)
    {
        if (cancelled)
        {
            return null;
        }

        if (finished)
        {
            return madeSeen ? null : "finished, but the table never said the plant was made (it said \"" + status + "\")";
        }

        return !madeSeen && mostPieces == 0 && piecesOffered ? "no piece stayed on the soil (the table said \"" + status + "\")" : null;
    }
}
