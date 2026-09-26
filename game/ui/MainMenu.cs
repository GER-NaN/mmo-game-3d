namespace MmoGame3d.Ui;

using System;
using Godot;
using MmoGame3d.Rules.Players;

// The first screen: which server, and Play. The character is chosen after connecting.
// It only reports what was pressed; ClientGame acts on it.
public partial class MainMenu : Control
{
    // (address).
    public event Action<string>? PlayPressed;
    public event Action? SettingsPressed;
    public event Action? QuitPressed;

    private LineEdit _address = null!;
    private Label _status = null!;
    private Button _play = null!;

    public override void _Ready()
    {
        _address = GetNode<LineEdit>("%Address");
        _status = GetNode<Label>("%Status");
        _play = GetNode<Button>("%Play");

        _play.Pressed += OnPlay;
        _address.TextSubmitted += _ => OnPlay();
        GetNode<Button>("%Settings").Pressed += () => SettingsPressed?.Invoke();
        GetNode<Button>("%Quit").Pressed += () => QuitPressed?.Invoke();
    }

    public void Fill(string profile, string address)
    {
        GetNode<Label>("%Profile").Text = "Profile: " + profile;
        _address.Text = address;
    }

    public void SetStatus(string text)
    {
        _status.Text = text;
    }

    // While connecting, Play is off so a second press does not start a second connection.
    public void SetBusy(bool busy)
    {
        _play.Disabled = busy;
    }

    private void OnPlay()
    {
        if (!_play.Disabled)
        {
            PlayPressed?.Invoke(_address.Text.Trim());
        }
    }
}
