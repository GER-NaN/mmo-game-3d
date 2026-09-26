namespace MmoGame3d.Ui;

using System;
using Godot;

// The selected player: their name, and what you can do to them. Buttons never take
// focus, so walking goes on after a click.
public partial class TargetFrame : PanelContainer
{
    // Bots find the Invite button by this group, then click it like a person.
    public const string InviteGroup = "target_invite";

    public event Action? InvitePressed;

    public override void _Ready()
    {
        GetNode<Button>("%Invite").AddToGroup(InviteGroup);
        GetNode<Button>("%Invite").Pressed += () => InvitePressed?.Invoke();
    }

    public void ShowTarget(string displayName, bool canInvite)
    {
        GetNode<Label>("%Name").Text = displayName;
        GetNode<Button>("%Invite").Visible = canInvite;
    }
}
