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

    public override void _Ready()
    {
        AddChild(new Compass { Name = "Compass" });
    }

    // The key shown in the prompt: the one "interact" is on.
    public string UseKey { get; set; } = "F";

    // The "[F] Go Online" line near the bottom; empty hides it.
    public void ShowPrompt(string prompt)
    {
        Label label = GetNode<Label>("%Prompt");
        label.Visible = prompt.Length > 0;
        label.Text = prompt.Length > 0 ? "[" + UseKey + "]  " + prompt : "";
    }

    public const string ActionGroupPrefix = "hud_action_";

    // The action bar, bottom right: the panels a key opens, also by mouse. Each button is
    // (action, label); the key it is on is shown with it.
    public event System.Action<string>? ActionPressed;

    private static readonly string[][] Actions =
    {
        new[] { "inventory", "Bag" },
        new[] { "skills", "Skills" },
        new[] { "social", "Friends" },
        new[] { "map", "Map" },
        new[] { "phone", "Phone" },
        new[] { "ui_cancel", "Menu" },
    };

    private static readonly Color BatteryGood = new Color(0.45f, 1f, 0.5f);
    private static readonly Color BatteryLow = new Color(1f, 0.85f, 0.35f);
    private static readonly Color BatteryDead = new Color(1f, 0.4f, 0.35f);

    private string _battery = "";
    private Color _batteryColor = Colors.White;

    // Keys as the settings have them; Esc for the menu.
    public void ShowActions(System.Func<string, string> keyName)
    {
        HBoxContainer bar = GetNodeOrNull<HBoxContainer>("Actions") ?? MakeBar();

        foreach (Node child in bar.GetChildren())
        {
            bar.RemoveChild(child);
            child.QueueFree();
        }

        foreach (string[] action in Actions)
        {
            string name = action[0];
            string key = name == "ui_cancel" ? "Esc" : keyName(name);
            string label = action[1] + (name == "phone" && _battery.Length > 0 ? " " + _battery : "");
            Button button = new Button { Text = label + "  [" + key + "]", FocusMode = FocusModeEnum.None };

            if (name == "phone" && _battery.Length > 0)
            {
                button.AddThemeColorOverride("font_color", _batteryColor);
            }

            button.AddToGroup(ActionGroupPrefix + name);
            button.Pressed += () => ActionPressed?.Invoke(name);
            bar.AddChild(button);
        }
    }

    // The equipped phone's battery, or no phone: -1.
    public void ShowBattery(int percent, System.Func<string, string> keyName)
    {
        _battery = percent < 0 ? "" : percent + "%";
        _batteryColor = percent <= 0 ? BatteryDead : (percent < 25 ? BatteryLow : BatteryGood);
        ShowActions(keyName);
    }

    private HBoxContainer MakeBar()
    {
        HBoxContainer bar = new HBoxContainer { Name = "Actions" };
        bar.AddThemeConstantOverride("separation", 6);
        bar.SetAnchorsPreset(LayoutPreset.BottomRight);
        bar.GrowHorizontal = GrowDirection.Begin;
        bar.GrowVertical = GrowDirection.Begin;
        bar.OffsetLeft = -16;
        bar.OffsetRight = -16;
        bar.OffsetTop = -84;
        bar.OffsetBottom = -48;
        AddChild(bar);
        return bar;
    }

    private int _shownHealth = -1;

    // HP under the name line, green, yellow below 50, red below 25.
    public void ShowHealth(int health)
    {
        if (health == _shownHealth)
        {
            return;
        }

        _shownHealth = health;
        ProgressBar? bar = GetNodeOrNull<ProgressBar>("Health");

        if (bar == null)
        {
            bar = new ProgressBar { Name = "Health", MinValue = 0, MaxValue = 100, ShowPercentage = false, MouseFilter = MouseFilterEnum.Ignore };
            bar.Position = new Vector2(16, 44);
            bar.Size = new Vector2(180, 14);
            AddChild(bar);
            Label label = new Label { Name = "HealthText", Position = new Vector2(202, 38), MouseFilter = MouseFilterEnum.Ignore };
            label.AddThemeConstantOverride("outline_size", 6);
            AddChild(label);
        }

        bar.Value = health;
        Color color = health < 25 ? new Color(0.95f, 0.3f, 0.25f) : (health < 50 ? new Color(0.95f, 0.8f, 0.3f) : new Color(0.35f, 0.85f, 0.4f));
        StyleBoxFlat fill = new StyleBoxFlat { BgColor = color };
        bar.AddThemeStyleboxOverride("fill", fill);
        GetNode<Label>("HealthText").Text = "HP " + health + (health < 50 ? "  (slowed)" : "");
    }

    // The job this player has taken, top right; empty hides it.
    public void ShowJob(string text)
    {
        Label? job = GetNodeOrNull<Label>("Job");

        if (job == null)
        {
            job = new Label { Name = "Job", MouseFilter = MouseFilterEnum.Ignore, HorizontalAlignment = HorizontalAlignment.Right };
            job.AddThemeConstantOverride("outline_size", 6);
            job.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.45f));
            job.SetAnchorsPreset(LayoutPreset.TopRight);
            job.GrowHorizontal = GrowDirection.Begin;
            job.OffsetLeft = -16;
            job.OffsetRight = -16;
            job.OffsetTop = 12;
            AddChild(job);
        }

        job.Visible = text.Length > 0;
        job.Text = text;
    }

    // The key help along the bottom.
    public void ShowHint(string hint)
    {
        GetNode<Label>("Hint").Text = hint;
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
