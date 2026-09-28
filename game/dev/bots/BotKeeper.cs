namespace MmoGame3d.Dev;

using System.Collections.Generic;
using System.IO;
using Godot;
using MmoGame3d.Players;
using MmoGame3d.Ui;

/// <summary>
/// Keeps a bot (--bot) in the game for a long run. It lives as long as the client, so it
/// sees the screens the bot itself does not: back at the main menu (a lost connection, a
/// refused login, a stray click on Leave) it waits for the server to let the old session
/// go, then clicks Play; the game menu left open too long, it clicks Resume. It acts
/// through clicks, like the bot.
///
/// A character switch (SwitchCharacterActivity) crosses the menus too, so the keeper
/// finishes it: Play on the main menu, then Play on the other character's card, or a
/// second character made (a random look, the name with " Two") when there is none. Back
/// in the world it judges the switch from the screen: the body must be the other one.
///
/// It also takes the bot's picture when tools/bot-watch asks: a request file named for
/// the profile holds the path to save to. The picture is the game's own view, so nothing
/// else on the screen is ever in it.
/// </summary>
public partial class BotKeeper : Node
{
    // Long enough for the server to drop the old session, or the login is refused again.
    private const double MenuWait = 15;

    // Longer than the bot's own look at the settings.
    private const double GameMenuWait = 10;

    private const double ShotCheckEvery = 1;

    // A clean leave lets the session go at once; a moment for the server all the same.
    private const double SwitchMenuWait = 3;
    private const double SwitchActEvery = 1.2;
    private const double SwitchTakesAtMost = 60;

    private string _switchFrom = "";
    private double _switchFor;
    private double _switchActIn;
    private int _creatorStage;
    private bool _leftWorld;

    // On the way through the main menu, the credits are read once a switch.
    private bool _creditsRead;

    // The Leave click may never land (the activity then fails): the switch is off.
    private const double LeaveWithin = 10;

    // Leaving to choose another character: the client shows the characters, not choosing.
    public bool Switching { get; private set; }

    public void Switch(string from)
    {
        Switching = true;
        _switchFrom = from;
        _switchFor = 0;
        _switchActIn = 0;
        _creatorStage = 0;
        _leftWorld = false;
        _creditsRead = false;
        GD.Print("Bot: switching characters from " + from);
    }

    private double _onMenuFor;
    private double _onGameMenuFor;
    private double _shotCheckIn;

    // The profile names the request file, as in tools/bot-watch.
    public string Profile { get; set; } = "";

    public const string Group = "bot_keeper";

    public override void _Ready()
    {
        AddToGroup(Group);
    }

    public override void _Process(double delta)
    {
        Button? play = GetTree().GetFirstNodeInGroup(MainMenu.PlayGroup) as Button;

        if (play != null && play.IsVisibleInTree() && !play.Disabled)
        {
            _onMenuFor += delta;

            if (Switching && !_creditsRead)
            {
                ReadCredits();
            }
            else if (_onMenuFor >= (Switching ? SwitchMenuWait : MenuWait))
            {
                _onMenuFor = 0;
                GD.Print("Bot: back at the main menu; clicking Play");
                BotDriver.Click(play.GetGlobalRect().GetCenter());
            }
        }
        else
        {
            _onMenuFor = 0;
        }

        Button? resume = GetTree().GetFirstNodeInGroup(InGameMenu.ResumeGroup) as Button;

        if (resume != null && resume.IsVisibleInTree())
        {
            _onGameMenuFor += delta;

            if (_onGameMenuFor >= GameMenuWait)
            {
                _onGameMenuFor = 0;
                GD.Print("Bot: the game menu is still open; clicking Resume");
                BotDriver.Click(resume.GetGlobalRect().GetCenter());
            }
        }
        else
        {
            _onGameMenuFor = 0;
        }

        if (Switching)
        {
            TickSwitch(delta);
        }

        _shotCheckIn -= delta;

        if (_shotCheckIn <= 0)
        {
            _shotCheckIn = ShotCheckEvery;
            TakeShotIfAsked();
        }
    }

    // Credits, a moment's look, Back; then the menu goes on to Play.
    private void ReadCredits()
    {
        Button? back = GetTree().GetFirstNodeInGroup(CreditsPanel.BackGroup) as Button;

        if (back == null)
        {
            Button? credits = GetTree().GetFirstNodeInGroup(MainMenu.CreditsGroup) as Button;

            if (credits != null && _onMenuFor >= 1)
            {
                _onMenuFor = 0;
                BotDriver.Click(credits.GetGlobalRect().GetCenter());
            }

            return;
        }

        if (_onMenuFor >= 2)
        {
            _onMenuFor = 0;
            _creditsRead = true;
            GD.Print("Bot: read the credits");
            BotDriver.Click(back.GetGlobalRect().GetCenter());
        }
    }

    private void TickSwitch(double delta)
    {
        _switchFor += delta;
        Player? me = GetTree().GetFirstNodeInGroup(Player.LocalGroup) as Player;

        // Left by the menus: the body alone going (a zone change) is not leaving.
        _leftWorld = _leftWorld
            || GetTree().GetFirstNodeInGroup(MainMenu.PlayGroup) != null
            || GetTree().GetFirstNodeInGroup(CharacterSelect.PlayGroup) != null
            || GetTree().GetFirstNodeInGroup(CharacterSelect.CreateGroup) != null;

        if (!_leftWorld)
        {
            if (_switchFor > LeaveWithin)
            {
                Switching = false;
                GD.Print("Bot: never left the world; not switching");
            }

            return;
        }

        if (me != null && me.DisplayName.Length > 0 && GetTree().GetFirstNodeInGroup(CharacterSelect.PlayGroup) == null)
        {
            Switching = false;
            string? problem = null;

            if (me.DisplayName == _switchFrom)
            {
                problem = "left " + _switchFrom + " to switch characters, and came back as " + _switchFrom;
            }
            else if (_switchFor > SwitchTakesAtMost)
            {
                problem = "switching from " + _switchFrom + " to " + me.DisplayName + " took " + (int)_switchFor + " s";
            }

            GD.Print("Bot: switched from " + _switchFrom + " to " + me.DisplayName + " in " + _switchFor.ToString("0.0") + " s");

            if (problem != null)
            {
                BotFindings.Write(me, Profile, "switch-failed", problem, new Dictionary<string, object?> { { "seconds", _switchFor } });
            }

            return;
        }

        _switchActIn -= delta;

        if (_switchActIn > 0)
        {
            return;
        }

        _switchActIn = SwitchActEvery;

        if (ChooseCharacter())
        {
            return;
        }

        MakeCharacter();
    }

    // Play on a card with another name, or Create on an empty one. True if it acted.
    private bool ChooseCharacter()
    {
        Button? create = GetTree().GetFirstNodeInGroup(CharacterSelect.CreateGroup) as Button;

        foreach (Node node in GetTree().GetNodesInGroup(CharacterSelect.NameGroup))
        {
            Label? name = node as Label;

            if (name == null || !name.IsVisibleInTree() || name.Text == _switchFrom)
            {
                continue;
            }

            foreach (Node sibling in name.GetParent().GetChildren())
            {
                Button? play = sibling as Button;

                if (play != null && play.IsInGroup(CharacterSelect.PlayGroup))
                {
                    GD.Print("Bot: playing " + name.Text);
                    BotDriver.Click(play.GetGlobalRect().GetCenter());
                    return true;
                }
            }
        }

        if (create != null && create.IsVisibleInTree() && GetTree().GetFirstNodeInGroup(CharacterCreator.NameGroup) == null)
        {
            GD.Print("Bot: no other character; making one");
            _creatorStage = 0;
            BotDriver.Click(create.GetGlobalRect().GetCenter());
            return true;
        }

        return false;
    }

    // The creator, a step each turn: a random look, the name, Done.
    private void MakeCharacter()
    {
        LineEdit? name = GetTree().GetFirstNodeInGroup(CharacterCreator.NameGroup) as LineEdit;

        if (name == null || !name.IsVisibleInTree())
        {
            return;
        }

        switch (_creatorStage)
        {
            case 0:
                Button? random = GetTree().GetFirstNodeInGroup(CharacterCreator.RandomGroup) as Button;

                if (random != null)
                {
                    BotDriver.Click(random.GetGlobalRect().GetCenter());
                }

                break;
            case 1:
                BotDriver.Click(name.GetGlobalRect().GetCenter());
                break;
            case 2:
                Input.ParseInputEvent(new InputEventKey { Keycode = Key.A, PhysicalKeycode = Key.A, CtrlPressed = true, Pressed = true });
                Input.ParseInputEvent(new InputEventKey { Keycode = Key.A, PhysicalKeycode = Key.A, CtrlPressed = true, Pressed = false });
                BotDriver.Type(_switchFrom + " Two");
                break;
            default:
                Button? done = GetTree().GetFirstNodeInGroup(CharacterCreator.DoneGroup) as Button;

                if (done != null)
                {
                    BotDriver.Click(done.GetGlobalRect().GetCenter());
                }

                break;
        }

        _creatorStage++;
    }

    private void TakeShotIfAsked()
    {
        string request = Path.Combine(Path.GetTempPath(), "mmo-game-3d-bots", "shot-" + Profile + ".request");

        if (!File.Exists(request))
        {
            return;
        }

        string target = File.ReadAllText(request).Trim();
        File.Delete(request);

        if (target.Length > 0)
        {
            GetViewport().GetTexture().GetImage().SavePng(target);
            GD.Print("Bot: picture saved to " + target);
        }
    }
}
