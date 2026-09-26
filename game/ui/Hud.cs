namespace MmoGame3d.Ui;

using Godot;

// What stays on screen while playing. It never takes the mouse, so clicks reach the world.
public partial class Hud : Control
{
    // A notice stays this long, then fades. At most this many show at once.
    private const float NoticeSeconds = 3f;
    private const float FadeSeconds = 0.6f;
    private const int MaxNotices = 5;

    public void ShowIdentity(string displayName, string zoneId)
    {
        GetNode<Label>("%Identity").Text = displayName + "   " + zoneId;
    }

    public void ShowNotice(string text)
    {
        VBoxContainer notices = GetNode<VBoxContainer>("%Notices");

        if (notices.GetChildCount() >= MaxNotices)
        {
            notices.GetChild(0).QueueFree();
        }

        Label label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        label.AddThemeConstantOverride("outline_size", 6);
        notices.AddChild(label);

        Tween tween = label.CreateTween();
        tween.TweenInterval(NoticeSeconds);
        tween.TweenProperty(label, "modulate:a", 0f, FadeSeconds);
        tween.TweenCallback(Callable.From(label.QueueFree));
    }
}
