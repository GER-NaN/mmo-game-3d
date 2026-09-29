namespace MmoGame3d.Ui;

using System;
using Godot;

// Esc in the world: where you are (server, player, zone), then the options. The game
// keeps running underneath: it is an online world, so there is no pause. Walking stops
// because the menu holds the keyboard focus.
public partial class InGameMenu : Control
{
    public event Action? ResumePressed;
    public event Action? SettingsPressed;
    public event Action? WardrobePressed;
    public event Action? LeavePressed;
    public event Action? QuitPressed;

    public override void _Ready()
    {
        GetNode<Button>("%Resume").Pressed += () => ResumePressed?.Invoke();
        GetNode<Button>("%Settings").Pressed += () => SettingsPressed?.Invoke();
        GetNode<Button>("%Wardrobe").Pressed += () => WardrobePressed?.Invoke();
        GetNode<Button>("%Leave").Pressed += () => LeavePressed?.Invoke();
        GetNode<Button>("%Quit").Pressed += () => QuitPressed?.Invoke();
        GetNode<Button>("%Resume").GrabFocus();
    }

    public void ShowStatus(string server, string player, string zone)
    {
        GetNode<Label>("%Server").Text = server;
        GetNode<Label>("%Player").Text = player;
        GetNode<Label>("%Zone").Text = zone;
    }
}
