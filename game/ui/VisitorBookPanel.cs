namespace MmoGame3d.Ui;

using System;
using Godot;

/// <summary>
/// The subway's visitor book: a page of names at a time, with every name ever sprayed on
/// the wall, oldest first. Opens at the newest page.
/// </summary>
public partial class VisitorBookPanel : PanelContainer
{
    // Bots find the page buttons by these groups.
    public const string PreviousGroup = "book_previous";
    public const string NextGroup = "book_next";

    private static readonly PackedScene CloseScene = GD.Load<PackedScene>("res://game/ui/CloseButton.tscn");

    public event Action<int>? PagePressed;
    public event Action? Closed;

    private VBoxContainer _names = null!;
    private Label _page = null!;
    private Button _previous = null!;
    private Button _next = null!;
    private int _shown;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(360, 0);
        SetAnchorsPreset(LayoutPreset.CenterRight);
        GrowHorizontal = GrowDirection.Begin;
        GrowVertical = GrowDirection.Both;
        OffsetRight = -24;
        OffsetLeft = -384;

        MarginContainer margin = new MarginContainer();

        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
        {
            margin.AddThemeConstantOverride(side, 14);
        }

        AddChild(margin);
        VBoxContainer rows = new VBoxContainer();
        rows.AddThemeConstantOverride("separation", 6);
        margin.AddChild(rows);

        HBoxContainer titleRow = new HBoxContainer();
        Label title = new Label { Text = "Visitor book", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        title.AddThemeFontSizeOverride("font_size", 22);
        titleRow.AddChild(title);
        Button close = CloseScene.Instantiate<Button>();
        close.Pressed += () => Closed?.Invoke();
        titleRow.AddChild(close);
        rows.AddChild(titleRow);
        rows.AddChild(new Label { Text = "Every name sprayed on the wall, in order.", Modulate = new Color(1f, 1f, 1f, 0.7f) });

        _names = new VBoxContainer();
        rows.AddChild(_names);

        HBoxContainer turn = new HBoxContainer();
        turn.AddThemeConstantOverride("separation", 8);
        _previous = new Button { Text = "<", FocusMode = FocusModeEnum.None };
        _previous.AddToGroup(PreviousGroup);
        _previous.Pressed += () => PagePressed?.Invoke(_shown - 1);
        _page = new Label { SizeFlagsHorizontal = SizeFlags.ExpandFill, HorizontalAlignment = HorizontalAlignment.Center };
        _next = new Button { Text = ">", FocusMode = FocusModeEnum.None };
        _next.AddToGroup(NextGroup);
        _next.Pressed += () => PagePressed?.Invoke(_shown + 1);
        turn.AddChild(_previous);
        turn.AddChild(_page);
        turn.AddChild(_next);
        rows.AddChild(turn);
    }

    public void ShowPage(int page, int pages, string[] lines)
    {
        _shown = page;

        foreach (Node old in _names.GetChildren())
        {
            old.QueueFree();
        }

        if (lines.Length == 0)
        {
            _names.AddChild(new Label { Text = "No names yet. Be the first." });
        }

        foreach (string line in lines)
        {
            _names.AddChild(new Label { Text = line });
        }

        _page.Text = "Page " + (page + 1) + " of " + pages;
        _previous.Disabled = page <= 0;
        _next.Disabled = page >= pages - 1;
    }
}
