namespace MmoGame3d.Ui;

using System;
using Godot;
using MmoGame3d.Client;

// The first screen: which server, from a list of named ones, and Play. The character is chosen after connecting.
// It only reports what was pressed; ClientGame acts on it.
public partial class MainMenu : Control
{
    private static readonly PackedScene CreditsScene = GD.Load<PackedScene>("res://game/ui/CreditsPanel.tscn");

    // (server name).
    public event Action<string>? PlayPressed;
    public event Action? SettingsPressed;
    public event Action? QuitPressed;

    private OptionButton _server = null!;
    private Label _status = null!;
    private Button _play = null!;

    public override void _Ready()
    {
        _server = GetNode<OptionButton>("%Server");

        foreach (GameServer server in ServerList.All)
        {
            _server.AddItem(server.Name);
        }

        _status = GetNode<Label>("%Status");
        _play = GetNode<Button>("%Play");

        _play.Pressed += OnPlay;
        GetNode<Button>("%Settings").Pressed += () => SettingsPressed?.Invoke();
        GetNode<Button>("%Credits").Pressed += ShowCredits;
        GetNode<Button>("%Quit").Pressed += () => QuitPressed?.Invoke();
    }

    public void Fill(string profile, string server)
    {
        GetNode<Label>("%Profile").Text = "Profile: " + profile;

        for (int i = 0; i < ServerList.All.Length; i++)
        {
            if (ServerList.All[i].Name == server)
            {
                _server.Select(i);
            }
        }
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
            PlayPressed?.Invoke(ServerList.All[Math.Max(_server.Selected, 0)].Name);
        }
    }

    private void ShowCredits()
    {
        CreditsPanel credits = CreditsScene.Instantiate<CreditsPanel>();
        AddChild(credits);
        credits.Closed += credits.QueueFree;
    }
}
