namespace MmoGame3d.Ui;

using System;
using Godot;

// Esc in the world. The game keeps running underneath: it is an online world, so
// there is no pause. Walking stops because the menu holds the keyboard focus.
public partial class InGameMenu : Control
{
    public event Action? ResumePressed;
    public event Action? SettingsPressed;
    public event Action? LeavePressed;
    public event Action? QuitPressed;

    public override void _Ready()
    {
        GetNode<Button>("%Resume").Pressed += () => ResumePressed?.Invoke();
        GetNode<Button>("%Settings").Pressed += () => SettingsPressed?.Invoke();
        GetNode<Button>("%Leave").Pressed += () => LeavePressed?.Invoke();
        GetNode<Button>("%Quit").Pressed += () => QuitPressed?.Invoke();
        GetNode<Button>("%Resume").GrabFocus();
    }
}
