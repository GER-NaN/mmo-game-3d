namespace MmoGame3d.Ui;

using System;
using Godot;

/// <summary>
/// The subway's visitor book: a page of names at a time, with every name ever sprayed on
/// the wall, oldest first. Opens at the newest page.
/// </summary>
public partial class VisitorBookPanel : PanelContainer
{
    public event Action<int>? PagePressed;
    public event Action? Closed;

    private VBoxContainer _names = null!;
    private Label _page = null!;
    private Button _previous = null!;
    private Button _next = null!;
    private int _shown;

    public override void _Ready()
    {
        _names = GetNode<VBoxContainer>("%Names");
        _page = GetNode<Label>("%Page");
        _previous = GetNode<Button>("%Previous");
        _next = GetNode<Button>("%Next");
        _previous.Pressed += () => PagePressed?.Invoke(_shown - 1);
        _next.Pressed += () => PagePressed?.Invoke(_shown + 1);
        GetNode<Button>("%Close").Pressed += () => Closed?.Invoke();
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
