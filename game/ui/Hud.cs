namespace MmoGame3d.Ui;

using Godot;

// What stays on screen while playing. It never takes the mouse, so clicks reach the world.
public partial class Hud : Control
{
    // A notice stays this long, then fades. At most this many show at once.
    private const float NoticeSeconds = 3f;
    private const float FadeSeconds = 0.6f;
    private const int MaxNotices = 5;

    private string _identity = "";
    private string _clock = "";
    private string _dollars = "";

    public void ShowIdentity(string displayName, string zoneId)
    {
        _identity = displayName + "   " + zoneId;
        Refresh();
    }

    public void ShowDollars(int dollars)
    {
        _dollars = "$" + dollars;
        Refresh();
    }

    public void ShowClock(string clock)
    {
        if (clock != _clock)
        {
            _clock = clock;
            Refresh();
        }
    }

    private void Refresh()
    {
        GetNode<Label>("%Identity").Text = _identity + (_clock.Length > 0 ? "   " + _clock : "") + (_dollars.Length > 0 ? "   " + _dollars : "");
    }

    // Bots read the prompt by this group, as a person reads the screen.
    public const string PromptGroup = "interact_prompt";

    public override void _Ready()
    {
        GetNode<Label>("%Prompt").AddToGroup(PromptGroup);
    }

    // The "F Go Online" line near the bottom; empty hides it.
    public void ShowPrompt(string prompt)
    {
        Label label = GetNode<Label>("%Prompt");
        label.Visible = prompt.Length > 0;
        label.Text = prompt.Length > 0 ? "[F]  " + prompt : "";
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
