namespace MmoGame3d.Ui;

using System;
using Godot;
using MmoGame3d.Rules.Players;

// The first screen: who you are, which server, and Play. It only reports what was
// pressed; ClientGame acts on it.
public partial class MainMenu : Control
{
    // (name, address, look).
    public event Action<string, string, string>? PlayPressed;
    public event Action? SettingsPressed;
    public event Action? QuitPressed;

    private LineEdit _name = null!;
    private LineEdit _address = null!;
    private Label _status = null!;
    private Button _play = null!;

    public override void _Ready()
    {
        _name = GetNode<LineEdit>("%Name");
        OptionButton look = GetNode<OptionButton>("%Look");

        foreach (string id in Looks.Ids)
        {
            look.AddItem(Looks.NameOf(id));
            look.SetItemMetadata(look.ItemCount - 1, id);
        }

        _address = GetNode<LineEdit>("%Address");
        _status = GetNode<Label>("%Status");
        _play = GetNode<Button>("%Play");

        _play.Pressed += OnPlay;
        _name.TextSubmitted += _ => OnPlay();
        GetNode<Button>("%Settings").Pressed += () => SettingsPressed?.Invoke();
        GetNode<Button>("%Quit").Pressed += () => QuitPressed?.Invoke();
    }

    public void Fill(string profile, string name, string address)
    {
        GetNode<Label>("%Profile").Text = "Profile: " + profile;
        _name.Text = name;
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
            OptionButton look = GetNode<OptionButton>("%Look");
            string id = look.Selected >= 0 ? (string)look.GetItemMetadata(look.Selected) : Looks.Default;
            PlayPressed?.Invoke(_name.Text, _address.Text.Trim(), id);
        }
    }
}
