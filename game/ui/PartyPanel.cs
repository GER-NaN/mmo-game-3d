namespace MmoGame3d.Ui;

using System;
using Godot;

// Your party: each member, the leader starred, the offline ones dimmed; and Leave. It
// folds down to its title, with the count, and opens again.
public partial class PartyPanel : PanelContainer
{
    public event Action? LeavePressed;

    private bool _folded;
    private int _count;

    public override void _Ready()
    {
        GetNode<Button>("%Leave").Pressed += () => LeavePressed?.Invoke();
        GetNode<Button>("%Fold").Pressed += () =>
        {
            _folded = !_folded;
            ShowFolded();
        };
    }

    private void ShowFolded()
    {
        GetNode<Control>("%Members").Visible = !_folded;
        GetNode<Control>("%Leave").Visible = !_folded;
        GetNode<Label>("%Title").Text = _folded ? "Party (" + _count + ")" : "Party";
        Button fold = GetNode<Button>("%Fold");
        fold.Text = _folded ? "+" : "-";
        fold.TooltipText = _folded ? "Show the party" : "Minimise";
        ResetSize();
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

        _count = ids.Length;
        ShowFolded();
    }
}
