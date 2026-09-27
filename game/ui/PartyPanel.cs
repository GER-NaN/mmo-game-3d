namespace MmoGame3d.Ui;

using System;
using Godot;

// Your party: each member, the leader starred, the offline ones dimmed; and Leave.
public partial class PartyPanel : PanelContainer
{
    // Bots find the Leave button by this group.
    public const string LeaveGroup = "party_leave";

    public event Action? LeavePressed;

    public override void _Ready()
    {
        GetNode<Button>("%Leave").Pressed += () => LeavePressed?.Invoke();
        GetNode<Button>("%Leave").AddToGroup(LeaveGroup);
    }

    public void ShowMembers(string leaderId, string[] ids, string[] names, int[] online)
    {
        VBoxContainer members = GetNode<VBoxContainer>("%Members");

        foreach (Node old in members.GetChildren())
        {
            old.QueueFree();
        }

        for (int i = 0; i < ids.Length && i < names.Length && i < online.Length; i++)
        {
            bool isOnline = online[i] != 0;
            Label label = new Label
            {
                Text = (ids[i] == leaderId ? "* " : "  ") + names[i] + (isOnline ? "" : "  (offline)"),
                Modulate = isOnline ? Colors.White : new Color(1f, 1f, 1f, 0.45f),
            };
            members.AddChild(label);
        }
    }
}
