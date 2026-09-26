namespace MmoGame3d.Dev;

using System;
using Godot;
using MmoGame3d.Players;
using MmoGame3d.Ui;

/// <summary>
/// Plays a client by itself, for load tests and headless checks. The rule, kept from
/// mmo-game: a bot may look things up, but it acts through input. It presses the same
/// actions and clicks the same buttons a person does, so everything past the keyboard
/// and mouse is the real game.
///
/// What it does: walks with pauses and turns, jumps sometimes, says a line in chat now
/// and then, clicks on a nearby player and invites them, joins any party it is invited
/// to, and goes online at a terminal it passes, then offline again a little later.
/// </summary>
public partial class BotDriver : Node
{
    // Placeholders for a wanderer; seconds.
    private const double MinSpell = 1.0;
    private const double MaxSpell = 4.0;
    private const float RecruitRange = 40f;

    // A person takes a moment to read before clicking. The pause also lets a new panel
    // lay itself out, so a button is where it is drawn when the click lands.
    private const double ReadDelay = 0.4;

    // How long the bot stays online before it goes offline again.
    private const double OnlineSeconds = 6;

    private static readonly string[] Lines =
    {
        "anyone seen a battery?",
        "this town needs more lights",
        "hello",
        "found some RAM over here",
    };

    private readonly Random _random = new Random();
    private double _spellLeft;
    private double _nextLine = 5;
    private double _nextRecruit = 4;
    private double _inviteClickIn = -1;
    private double _joinSeenFor;
    private double _onlineFor;
    private double _interactHeldFor = -1;
    private double _nextInteract;

    // How the bot talks: the same call the chat box makes.
    public Action<string>? Say { get; set; }

    public override void _Process(double delta)
    {
        AcceptInvites(delta);
        UseTerminals(delta);

        // Online, the body stands still and the keys belong to the terminal.
        if (GetTree().GetFirstNodeInGroup(TerminalScreen.GoOfflineGroup) != null)
        {
            return;
        }

        Recruit(delta);
        Talk(delta);
        Wander(delta);
    }

    public override void _ExitTree()
    {
        ReleaseKeys();
    }

    private void AcceptInvites(double delta)
    {
        Button? join = GetTree().GetFirstNodeInGroup(InvitePrompt.JoinGroup) as Button;

        if (join == null || !join.IsVisibleInTree())
        {
            _joinSeenFor = 0;
            return;
        }

        _joinSeenFor += delta;

        if (_joinSeenFor >= ReadDelay)
        {
            _joinSeenFor = 0;
            GD.Print("Bot: clicking Join");
            Click(join.GetGlobalRect().GetCenter());
        }
    }

    // Two steps: click the body, then, once the target frame has shown, click Invite.
    private void Recruit(double delta)
    {
        if (_inviteClickIn >= 0)
        {
            _inviteClickIn -= delta;

            if (_inviteClickIn >= 0)
            {
                return;
            }

            Button? invite = GetTree().GetFirstNodeInGroup(TargetFrame.InviteGroup) as Button;

            if (invite != null && invite.IsVisibleInTree())
            {
                GD.Print("Bot: clicking Invite");
                Click(invite.GetGlobalRect().GetCenter());
            }
            else
            {
                GD.Print("Bot: no Invite button (nobody selected, or already in a party)");
            }

            return;
        }

        _nextRecruit -= delta;

        if (_nextRecruit > 0)
        {
            return;
        }

        _nextRecruit = 4 + (_random.NextDouble() * 4);
        Player? self = GetTree().GetFirstNodeInGroup(Player.LocalGroup) as Player;
        Camera3D? camera = GetViewport().GetCamera3D();

        if (self == null || camera == null)
        {
            return;
        }

        foreach (Node node in self.GetParent().GetChildren())
        {
            Player? other = node as Player;

            if (other == null || other == self || other.GlobalPosition.DistanceTo(self.GlobalPosition) > RecruitRange)
            {
                continue;
            }

            Vector3 chest = other.GlobalPosition + new Vector3(0f, 1f, 0f);

            if (!camera.IsPositionBehind(chest))
            {
                GD.Print("Bot: clicking on " + other.DisplayName);
                Click(camera.UnprojectPosition(chest));
                _inviteClickIn = ReadDelay;
                return;
            }
        }
    }

    private void UseTerminals(double delta)
    {
        // Held for a moment, like a key press, so the game sees it down then up.
        if (_interactHeldFor >= 0)
        {
            _interactHeldFor += delta;

            if (_interactHeldFor >= 0.1)
            {
                _interactHeldFor = -1;
                Input.ActionRelease("interact");
            }
        }

        _nextInteract -= delta;

        Button? goOffline = GetTree().GetFirstNodeInGroup(TerminalScreen.GoOfflineGroup) as Button;

        if (goOffline != null)
        {
            _onlineFor += delta;

            if (_onlineFor >= OnlineSeconds && goOffline.IsVisibleInTree())
            {
                _onlineFor = 0;
                GD.Print("Bot: clicking Go Offline");
                Click(goOffline.GetGlobalRect().GetCenter());
            }

            return;
        }

        _onlineFor = 0;
        Label? prompt = GetTree().GetFirstNodeInGroup(Hud.PromptGroup) as Label;

        if (prompt != null && prompt.Visible && prompt.Text.Contains("Go Online") && _nextInteract <= 0)
        {
            GD.Print("Bot: pressing F at \"" + prompt.Text + "\"");
            ReleaseKeys();
            Input.ActionPress("interact");
            _interactHeldFor = 0;
            _nextInteract = 2;
        }
    }

    private void Talk(double delta)
    {
        _nextLine -= delta;

        if (_nextLine <= 0 && Say != null)
        {
            _nextLine = 15 + (_random.NextDouble() * 15);
            Say(Lines[_random.Next(Lines.Length)]);
        }
    }

    private void Wander(double delta)
    {
        _spellLeft -= delta;

        if (_spellLeft > 0)
        {
            return;
        }

        _spellLeft = MinSpell + (_random.NextDouble() * (MaxSpell - MinSpell));
        ReleaseKeys();

        int choice = _random.Next(10);

        switch (choice)
        {
            case 0:
                // Stand still for a spell.
                break;
            case 1:
            case 2:
                Input.ActionPress("move_forward");
                Input.ActionPress(_random.Next(2) == 0 ? "turn_left" : "turn_right");
                break;
            case 3:
                Input.ActionPress("move_forward");
                Input.ActionPress("jump");
                break;
            default:
                Input.ActionPress("move_forward");
                break;
        }
    }

    // A real press and release at a screen point, through the same input queue a mouse
    // feeds, so the GUI and the picker both see it.
    private static void Click(Vector2 at)
    {
        InputEventMouseButton press = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = at, GlobalPosition = at };
        InputEventMouseButton release = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = at, GlobalPosition = at };
        Input.ParseInputEvent(press);
        Input.ParseInputEvent(release);
    }

    private static void ReleaseKeys()
    {
        Input.ActionRelease("move_forward");
        Input.ActionRelease("turn_left");
        Input.ActionRelease("turn_right");
        Input.ActionRelease("jump");
    }
}
