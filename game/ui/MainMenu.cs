namespace MmoGame3d.Ui;

using System;
using Godot;

// The first screen: which server, and Play. The character is chosen after connecting.
// It only reports what was pressed; ClientGame acts on it.
public partial class MainMenu : Control
{
    private static readonly PackedScene CreditsScene = GD.Load<PackedScene>("res://game/ui/CreditsPanel.tscn");

    // Bots find the Play button by this group.
    public const string PlayGroup = "main_menu_play";
    public const string CreditsGroup = "main_menu_credits";

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
        _play.AddToGroup(PlayGroup);
        _address.TextSubmitted += _ => OnPlay();
        GetNode<Button>("%Settings").Pressed += () => SettingsPressed?.Invoke();
        GetNode<Button>("%Credits").Pressed += ShowCredits;
        GetNode<Button>("%Credits").AddToGroup(CreditsGroup);
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

    private void ShowCredits()
    {
        CreditsPanel credits = CreditsScene.Instantiate<CreditsPanel>();
        AddChild(credits);
        credits.Closed += credits.QueueFree;
    }
}
