namespace MmoGame3d.Ui;

using System;
using Godot;
using MmoGame3d.Rules.Parties;

// "X invites you to a party." It closes itself when the invite would have expired on
// the server anyway, so an old prompt never offers an invite that is gone.
public partial class InvitePrompt : PanelContainer
{
    private double _secondsLeft = PartyRoster.InviteLifetimeSeconds;

    // true to join, false to decline.
    public event Action<bool>? Answered;

    public override void _Ready()
    {
        GetNode<Button>("%Join").Pressed += () => Answer(true);
        GetNode<Button>("%No").Pressed += () => Answer(false);
    }

    public void ShowInvite(string inviterName)
    {
        GetNode<Label>("%Text").Text = inviterName + " invites you to a party.";
    }

    public override void _Process(double delta)
    {
        _secondsLeft -= delta;

        if (_secondsLeft <= 0)
        {
            QueueFree();
        }
    }

    private void Answer(bool join)
    {
        Answered?.Invoke(join);
        QueueFree();
    }
}
