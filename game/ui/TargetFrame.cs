namespace MmoGame3d.Ui;

using System;
using Godot;

// The selected player: their name, and what you can do to them. Buttons never take
// focus, so walking goes on after a click.
public partial class TargetFrame : PanelContainer
{
    // Bots find the Invite and Give buttons by these groups, then click them like a person.
    public const string InviteGroup = "target_invite";
    public const string GiveGroup = "target_give";
    public const string FriendGroup = "target_friend";

    public event Action? InvitePressed;
    public event Action? GivePressed;
    public event Action? FriendPressed;
    public event Action? IgnorePressed;

    public override void _Ready()
    {
        GetNode<Button>("%Invite").AddToGroup(InviteGroup);
        GetNode<Button>("%Invite").Pressed += () => InvitePressed?.Invoke();
        GetNode<Button>("%Give").AddToGroup(GiveGroup);
        GetNode<Button>("%Give").Pressed += () => GivePressed?.Invoke();
        GetNode<Button>("%Friend").AddToGroup(FriendGroup);
        GetNode<Button>("%Friend").Pressed += () => FriendPressed?.Invoke();
        GetNode<Button>("%Ignore").Pressed += () => IgnorePressed?.Invoke();
    }

    public void ShowTarget(string displayName, bool canInvite)
    {
        GetNode<Label>("%Name").Text = displayName;
        GetNode<Button>("%Invite").Visible = canInvite;
    }
}
