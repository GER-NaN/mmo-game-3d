namespace MmoGame3d.Ui;

using Godot;

/// <summary>
/// A drawn computer mouse with its left button, wheel and right button in their own
/// colours, for a controls key beside it that uses the same colours.
/// </summary>
public partial class MouseSketch : Control
{
    public static readonly Color LeftColor = new Color(0.55f, 0.85f, 1f);
    public static readonly Color WheelColor = new Color(1f, 0.8f, 0.35f);
    public static readonly Color RightColor = new Color(0.7f, 1f, 0.6f);

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(56, 84);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Draw()
    {
        Vector2 size = new Vector2(52, 80);
        Vector2 at = (Size - size) / 2f;
        float split = size.Y * 0.42f;

        StyleBoxFlat body = new StyleBoxFlat { BgColor = new Color(0.16f, 0.18f, 0.22f), BorderColor = new Color(0.85f, 0.88f, 0.92f) };
        body.SetBorderWidthAll(2);
        body.SetCornerRadiusAll(24);
        DrawStyleBox(body, new Rect2(at, size));

        StyleBoxFlat left = new StyleBoxFlat { BgColor = LeftColor };
        left.CornerRadiusTopLeft = 22;
        DrawStyleBox(left, new Rect2(at + new Vector2(3, 3), new Vector2((size.X / 2f) - 4, split - 3)));

        StyleBoxFlat right = new StyleBoxFlat { BgColor = RightColor };
        right.CornerRadiusTopRight = 22;
        DrawStyleBox(right, new Rect2(at + new Vector2((size.X / 2f) + 1, 3), new Vector2((size.X / 2f) - 4, split - 3)));

        StyleBoxFlat wheel = new StyleBoxFlat { BgColor = WheelColor, BorderColor = new Color(0.16f, 0.18f, 0.22f) };
        wheel.SetBorderWidthAll(2);
        wheel.SetCornerRadiusAll(5);
        DrawStyleBox(wheel, new Rect2(at + new Vector2((size.X / 2f) - 6, 10), new Vector2(12, 22)));
    }
}
