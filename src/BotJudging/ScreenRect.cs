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
