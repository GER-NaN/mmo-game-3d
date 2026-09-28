namespace MmoGame3d.Ui;

using System;
using Godot;

// The selected player: their name, and what you can do to them. Buttons never take
// focus, so walking goes on after a click.
public partial class TargetFrame : PanelContainer
{
    public event Action? InvitePressed;
    public event Action? GivePressed;
    public event Action? FriendPressed;
    public event Action? IgnorePressed;
    public event Action? MessagePressed;

    public override void _Ready()
    {
        GetNode<Button>("%Invite").Pressed += () => InvitePressed?.Invoke();
        GetNode<Button>("%Give").Pressed += () => GivePressed?.Invoke();
        GetNode<Button>("%Friend").Pressed += () => FriendPressed?.Invoke();
        GetNode<Button>("%Ignore").Pressed += () => IgnorePressed?.Invoke();
        GetNode<Button>("%Message").Pressed += () => MessagePressed?.Invoke();
    }

    public void ShowTarget(string displayName, bool canInvite)
    {
        GetNode<Label>("%Name").Text = displayName;
        GetNode<Button>("%Invite").Visible = canInvite;
    }
}
