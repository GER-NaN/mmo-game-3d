namespace MmoGame3d.Ui;

using Godot;

/// <summary>
/// A strip at the top of the screen that shows which way the camera looks: N, NE, E and
/// so on slide past a mark in the middle. North is -Z in every zone (the town's north
/// edge is at -Z). The look is a placeholder.
/// </summary>
public partial class Compass : Control
{
    // Placeholders until seen in the game.
    private const float Width = 360f;
    private const float Height = 34f;
    private const float PixelsPerDegree = 2f;
    private const int FontSize = 16;

    private static readonly string[] Points = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
    private static readonly Color Back = new Color(0f, 0f, 0f, 0.35f);
    private static readonly Color Tick = new Color(1f, 1f, 1f, 0.5f);
    private static readonly Color Letter = new Color(1f, 1f, 1f, 0.9f);
    private static readonly Color North = new Color(1f, 0.45f, 0.35f);

    private float _bearing;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.CenterTop);
        OffsetLeft = -Width / 2f;
        OffsetRight = Width / 2f;
        OffsetTop = 10f;
        OffsetBottom = 10f + Height;
    }

    public override void _Process(double delta)
    {
        Camera3D? camera = GetViewport().GetCamera3D();

        if (camera == null)
        {
            return;
        }

        // Degrees clockwise from north, seen from above: -Z is 0, +X is 90.
        Vector3 forward = -camera.GlobalBasis.Z;
        float bearing = Mathf.PosMod(Mathf.RadToDeg(Mathf.Atan2(forward.X, -forward.Z)), 360f);

        if (!Mathf.IsEqualApprox(bearing, _bearing))
        {
            _bearing = bearing;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), Back);
        Font font = ThemeDB.FallbackFont;
        float middle = Size.X / 2f;

        for (int degrees = 0; degrees < 360; degrees += 15)
        {
            float off = Mathf.Wrap(degrees - _bearing, -180f, 180f) * PixelsPerDegree;

            if (Mathf.Abs(off) > middle - 8f)
            {
                continue;
            }

            float x = middle + off;

            if (degrees % 45 == 0)
            {
                string text = Points[degrees / 45];
                Vector2 size = font.GetStringSize(text, HorizontalAlignment.Left, -1, FontSize);
                DrawString(font, new Vector2(x - (size.X / 2f), (Height / 2f) + (size.Y / 3f)), text, HorizontalAlignment.Left, -1, FontSize, degrees == 0 ? North : Letter);
            }
            else
            {
                DrawLine(new Vector2(x, Height - 9f), new Vector2(x, Height - 3f), Tick, 1f);
            }
        }

        DrawLine(new Vector2(middle, 0f), new Vector2(middle, 5f), Letter, 2f);
    }
}
