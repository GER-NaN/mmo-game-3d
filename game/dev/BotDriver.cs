namespace MmoGame3d.Dev;

using System;
using Godot;
using MmoGame3d.Players;
using MmoGame3d.Rules.Terminals;
using MmoGame3d.Ui;

/// <summary>
/// Plays a client by itself, for load tests and headless checks. The rule, kept from
/// mmo-game: a bot may look things up, but it acts through input. It presses the same
/// actions and clicks the same buttons a person does, so everything past the keyboard
/// and mouse is the real game.
///
/// What it does: walks with pauses and turns, jumps sometimes, says a line in chat now
/// and then, clicks on a nearby player and invites them, joins any party it is invited
/// to, goes online at a terminal it passes (then offline again a little later), buys
/// the first thing a shopkeeper offers, opens chests, equips its phone and goes online on it, and at a
/// workbench takes the battery out and puts one in. It drops a stack once, and gives one
/// thing to a party member it clicks on, and adds the first player it clicks on as a
/// friend. It presses R (the EMP) every few seconds. At a public terminal it cracks one code, guessing only codes that still fit
/// every answer so far, and types each guess. Now and then it glances at the map. Once, as it
/// arrives, it looks at the settings and closes them unchanged (the settings file is the
/// machine's, shared with the person who plays on it), then at its friends list.
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
    private double _nextRecruit = 1.5;
    private double _inviteClickIn = -1;
    private double _joinSeenFor;
    private double _onlineFor;
    private double _shopSeenFor;
    private bool _boughtHere;
    private double _nextPhoneStep = 6;
    private int _phoneStep;
    private double _benchSeenFor;
    private int _benchClicks;
    private double _giveClickIn = -1;
    private bool _dropped;
    private bool _befriended;
    private bool _packOpened;
    private bool _messaged;
    private double _nextEmp = 1;
    private double _typeIn = -1;
    private int _crackStep;
    private int _whoisStep;
    private double _collegeSeenFor;
    private double _crackIn;
    private int _crackSeen;
    private double _interactHeldFor = -1;
    private double _nextInteract;
    private double _nextMap = 1;
    private int _settingsStep;
    private double _settingsIn = 0.5;
    private bool _mapOpen;

    // How the bot talks: the same call the chat box makes.
    public Action<string>? Say { get; set; }

    public override void _Process(double delta)
    {
        // A private message: the chat opened on the conversation; type into it.
        if (_typeIn >= 0)
        {
            _typeIn -= delta;

            if (_typeIn < 0)
            {
                Type("hi");
            }
        }

        if (LookAtSettings(delta))
        {
            return;
        }

        AcceptInvites(delta);

        // Reading a college panel, it stands still: walking off would close it.
        if (College(delta))
        {
            return;
        }

        Shop(delta);
        Workbench(delta);
        UseTerminals(delta);

        // Online, the body stands still and the keys belong to the terminal.
        if (GetTree().GetFirstNodeInGroup(TerminalScreen.GoOfflineGroup) != null)
        {
            return;
        }

        UsePhone(delta);
        GlanceAtMap(delta);
        FireEmp(delta);
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

    // Two steps: click the body, then, once the target frame has shown, click Invite, or
    // Give when already in a party with them, and then the first thing to give.
    private void Recruit(double delta)
    {
        if (_giveClickIn >= 0)
        {
            _giveClickIn -= delta;

            if (_giveClickIn < 0)
            {
                Button? giveOne = GetTree().GetFirstNodeInGroup(GivePanel.GiveGroup) as Button;

                if (giveOne != null && giveOne.IsVisibleInTree())
                {
                    GD.Print("Bot: clicking Give 1");
                    Click(giveOne.GetGlobalRect().GetCenter());
                }

                Press("ui_cancel");
            }

            return;
        }

        if (_inviteClickIn >= 0)
        {
            _inviteClickIn -= delta;

            if (_inviteClickIn >= 0)
            {
                return;
            }

            Button? invite = GetTree().GetFirstNodeInGroup(TargetFrame.InviteGroup) as Button;

            Button? give = GetTree().GetFirstNodeInGroup(TargetFrame.GiveGroup) as Button;
            Button? friend = GetTree().GetFirstNodeInGroup(TargetFrame.FriendGroup) as Button;
            Button? message = GetTree().GetFirstNodeInGroup(TargetFrame.MessageGroup) as Button;

            if (!_befriended && friend != null && friend.IsVisibleInTree())
            {
                _befriended = true;
                GD.Print("Bot: clicking Add friend");
                Click(friend.GetGlobalRect().GetCenter());
            }
            else if (!_messaged && message != null && message.IsVisibleInTree())
            {
                _messaged = true;
                GD.Print("Bot: clicking Message");
                Click(message.GetGlobalRect().GetCenter());
                _typeIn = ReadDelay;
            }
            else if (invite != null && invite.IsVisibleInTree())
            {
                GD.Print("Bot: clicking Invite");
                Click(invite.GetGlobalRect().GetCenter());
            }
            else if (give != null && give.IsVisibleInTree())
            {
                GD.Print("Bot: clicking Give");
                Click(give.GetGlobalRect().GetCenter());
                _giveClickIn = ReadDelay;
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
            TakeJob();

            if (CrackCode(delta) || LookAtWhois())
            {
                return;
            }

            if (_onlineFor >= OnlineSeconds && goOffline.IsVisibleInTree())
            {
                _onlineFor = 0;
                _crackStep = 0;
                GD.Print("Bot: clicking Go Offline");
                Click(goOffline.GetGlobalRect().GetCenter());
            }

            return;
        }

        _onlineFor = 0;
        Label? prompt = GetTree().GetFirstNodeInGroup(Hud.PromptGroup) as Label;

        bool usable = prompt != null && prompt.Visible
            && (prompt.Text.Contains("Go Online") || prompt.Text.Contains("Talk to") || prompt.Text.Contains("workbench") || prompt.Text.Contains("Repair") || prompt.Text.Contains("Open the") || prompt.Text.Contains("robo taxi") || prompt.Text.Contains("Fix the"));

        // Reading the map, it does not stop to use things.
        if (prompt != null && usable && _nextInteract <= 0 && !_mapOpen)
        {
            GD.Print("Bot: pressing F at \"" + prompt.Text + "\"");
            ReleaseKeys();
            Input.ActionPress("interact");
            _interactHeldFor = 0;
            _nextInteract = 2;
        }
    }

    // True while it is still cracking. Steps: open the app, start a code, then guess
    // each time a new answer is on the screen, until the code is cracked or locked.
    private bool CrackCode(double delta)
    {
        Button? app = GetTree().GetFirstNodeInGroup(TerminalScreen.AppGroupPrefix + TerminalApps.CodeCracker) as Button;
        TerminalScreen? screen = GetTree().GetFirstNodeInGroup(TerminalScreen.GoOfflineGroup)?.Owner as TerminalScreen;

        // Town repairs goes first; it opens its app in the first moments online.
        if (app == null || screen == null || _crackStep < 0 || _onlineFor < ReadDelay * 3)
        {
            return false;
        }

        _crackIn -= delta;

        if (_crackIn > 0)
        {
            return true;
        }

        _crackIn = ReadDelay;

        switch (_crackStep)
        {
            case 0:
                GD.Print("Bot: opening the code cracker");
                Click(app.GetGlobalRect().GetCenter());
                _crackStep = 1;
                return true;
            case 1:
                Button? start = GetTree().GetFirstNodeInGroup(TerminalScreen.CrackStartGroup) as Button;

                if (start != null && start.IsVisibleInTree())
                {
                    Click(start.GetGlobalRect().GetCenter());
                    _crackSeen = -1;
                    _crackStep = 2;
                }

                return true;
            default:
                string[]? guesses = screen.CrackGuesses;

                if (guesses == null || guesses.Length == _crackSeen)
                {
                    return true;
                }

                if (screen.CrackStatus != 0)
                {
                    GD.Print("Bot: code " + (screen.CrackStatus == 1 ? "cracked" : "locked out") + " in " + guesses.Length + " guesses");
                    _crackStep = -1;
                    return false;
                }

                _crackSeen = guesses.Length;
                Type(NextGuess(guesses, screen.CrackExact, screen.CrackPartial));
                return true;
        }
    }

    // Once, after a code: open Whois and look at its own page for a few seconds.
    private bool LookAtWhois()
    {
        Button? app = GetTree().GetFirstNodeInGroup(TerminalScreen.AppGroupPrefix + TerminalApps.Whois) as Button;

        if (app == null || _crackStep != -1 || _whoisStep > 2)
        {
            return false;
        }

        _crackIn -= GetProcessDeltaTime();

        if (_crackIn > 0)
        {
            return true;
        }

        _crackIn = ReadDelay;

        switch (_whoisStep)
        {
            case 0:
                GD.Print("Bot: opening Whois");
                Click(app.GetGlobalRect().GetCenter());
                break;
            case 1:
                Button? mine = GetTree().GetFirstNodeInGroup(TerminalScreen.WhoisMineGroup) as Button;

                if (mine != null)
                {
                    GD.Print("Bot: clicking My page");
                    Click(mine.GetGlobalRect().GetCenter());
                }

                _crackIn = 15;
                break;
            default:
                _onlineFor = 0;
                break;
        }

        _whoisStep++;
        return _whoisStep <= 2;
    }

    // The first code, in order, that would have given every answer seen so far.
    private static string NextGuess(string[] guesses, int[] exact, int[] partial)
    {
        int count = 1;

        for (int i = 0; i < CodeCracker.Length; i++)
        {
            count *= CodeCracker.Digits;
        }

        for (int n = 0; n < count; n++)
        {
            char[] digits = new char[CodeCracker.Length];
            int rest = n;

            for (int i = CodeCracker.Length - 1; i >= 0; i--)
            {
                digits[i] = (char)('0' + (rest % CodeCracker.Digits));
                rest /= CodeCracker.Digits;
            }

            string candidate = new string(digits);
            bool fits = true;

            for (int g = 0; g < guesses.Length && fits; g++)
            {
                CodeCracker check = new CodeCracker(candidate);
                check.Guess(guesses[g]);
                fits = check.Exact[0] == exact[g] && check.Partial[0] == partial[g];
            }

            if (fits)
            {
                return candidate;
            }
        }

        return "0000";
    }

    // Key by key into whatever has the focus, then Enter.
    private static void Type(string text)
    {
        foreach (char c in text)
        {
            Key key = char.IsDigit(c) ? Key.Key0 + (c - '0') : Key.A + (char.ToUpperInvariant(c) - 'A');
            Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Unicode = c, Pressed = true });
            Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Unicode = c, Pressed = false });
        }

        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Enter, PhysicalKeycode = Key.Enter, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Enter, PhysicalKeycode = Key.Enter, Pressed = false });
    }

    // Online: open Town repairs, and take the job if it is offered.
    private void TakeJob()
    {
        Button? take = GetTree().GetFirstNodeInGroup(TerminalScreen.TakeJobGroup) as Button;

        if (take != null && take.IsVisibleInTree())
        {
            if (_onlineFor > ReadDelay * 3)
            {
                GD.Print("Bot: clicking Take the job");
                Click(take.GetGlobalRect().GetCenter());
                _onlineFor = ReadDelay;
            }

            return;
        }

        if (_onlineFor > ReadDelay && _onlineFor < ReadDelay * 2)
        {
            foreach (Node node in GetTree().Root.FindChildren("*", "Button", true, false))
            {
                Button? app = node as Button;

                if (app != null && app.Text == "Town repairs" && app.IsVisibleInTree())
                {
                    GD.Print("Bot: opening Town repairs");
                    Click(app.GetGlobalRect().GetCenter());
                    _onlineFor = ReadDelay * 2;
                    return;
                }
            }
        }
    }

    // With a shop open: after a moment, buy the first offer once, then close the shop.
    private void Shop(double delta)
    {
        Button? buy = GetTree().GetFirstNodeInGroup(ShopPanel.BuyGroup) as Button;

        if (buy == null || !buy.IsVisibleInTree())
        {
            _shopSeenFor = 0;
            _boughtHere = false;
            return;
        }

        _shopSeenFor += delta;

        if (_shopSeenFor < ReadDelay)
        {
            return;
        }

        if (!_boughtHere)
        {
            _boughtHere = true;
            GD.Print("Bot: clicking Buy");
            Click(buy.GetGlobalRect().GetCenter());
        }
        else if (_shopSeenFor > ReadDelay * 4)
        {
            GD.Print("Bot: closing the shop");
            Press("ui_cancel");
        }
    }

    // Open the inventory, equip the phone if it is loose, close the inventory, then go
    // online on the phone now and then. The terminal step clicks Go Offline later.
    private void UsePhone(double delta)
    {
        _nextPhoneStep -= delta;

        if (_nextPhoneStep > 0)
        {
            return;
        }

        _nextPhoneStep = 1.5;

        switch (_phoneStep)
        {
            case 0:
                Press("inventory");
                _phoneStep = 1;
                break;
            case 1:
                // An engineer tries their repair pack once; the workbench step does the work.
                Button? pack = GetTree().GetFirstNodeInGroup(InventoryPanel.RepairPackGroup) as Button;

                if (!_packOpened && pack != null && pack.IsVisibleInTree())
                {
                    _packOpened = true;
                    GD.Print("Bot: clicking Open repair pack");
                    Click(pack.GetGlobalRect().GetCenter());
                    _phoneStep = 3;
                    _nextPhoneStep = 8;
                    break;
                }

                Button? equip = GetTree().GetFirstNodeInGroup(InventoryPanel.EquipGroup) as Button;

                if (equip != null && equip.IsVisibleInTree())
                {
                    GD.Print("Bot: clicking Equip");
                    Click(equip.GetGlobalRect().GetCenter());
                }

                _phoneStep = 2;
                break;
            case 2:
                Button? drop = GetTree().GetFirstNodeInGroup(InventoryPanel.DropGroup) as Button;

                if (!_dropped && drop != null && drop.IsVisibleInTree())
                {
                    _dropped = true;
                    GD.Print("Bot: clicking Drop");
                    Click(drop.GetGlobalRect().GetCenter());
                }

                Press("inventory");
                _phoneStep = 3;
                break;
            default:
                GD.Print("Bot: pressing P");
                Press("phone");
                _nextPhoneStep = 20;
                break;
        }
    }

    // At the college: finish the Class, enroll in the first open career, take a rank when
    // it is offered. One click per read.
    private bool College(double delta)
    {
        Button? button = FirstUsable(CollegePanel.ClassGroup) ?? FirstUsable(CollegePanel.EnrollGroup) ?? FirstUsable(CollegePanel.RankUpGroup);

        if (button == null)
        {
            _collegeSeenFor = 0;
            return false;
        }

        ReleaseKeys();

        _collegeSeenFor += delta;

        if (_collegeSeenFor >= ReadDelay * 2)
        {
            GD.Print("Bot: clicking " + button.Text);
            Click(button.GetGlobalRect().GetCenter());
            _collegeSeenFor = -ReadDelay * 2;
        }

        return true;
    }

    private Button? FirstUsable(string group)
    {
        foreach (Node node in GetTree().GetNodesInGroup(group))
        {
            Button? button = node as Button;

            if (button != null && !button.Disabled && button.IsVisibleInTree())
            {
                return button;
            }
        }

        return null;
    }

    // Esc, Settings, a look, Back, Esc; then the friends list open and shut. True while
    // it is still at it.
    private bool LookAtSettings(double delta)
    {
        if (_settingsStep > 5)
        {
            return false;
        }

        _settingsIn -= delta;

        if (_settingsIn > 0)
        {
            return true;
        }

        switch (_settingsStep)
        {
            case 0:
                Press("ui_cancel");
                _settingsIn = ReadDelay;
                break;
            case 1:
                Button? settings = GetTree().GetFirstNodeInGroup(InGameMenu.SettingsGroup) as Button;

                if (settings != null)
                {
                    Click(settings.GetGlobalRect().GetCenter());
                }

                _settingsIn = 3;
                break;
            case 2:
                Button? back = GetTree().GetFirstNodeInGroup(SettingsPanel.BackGroup) as Button;

                if (back != null)
                {
                    Click(back.GetGlobalRect().GetCenter());
                }

                _settingsIn = ReadDelay;
                break;
            case 3:
                Press("ui_cancel");
                _settingsIn = ReadDelay;
                break;
            case 4:
                Press("social");
                _settingsIn = 2;
                break;
            default:
                Press("social");
                break;
        }

        _settingsStep++;
        return true;
    }

    // Presses R now and then: with an emitter equipped, drones in range come down.
    private void FireEmp(double delta)
    {
        _nextEmp -= delta;

        if (_nextEmp <= 0)
        {
            _nextEmp = 5;
            Press("emp");
        }
    }

    // Open for a few seconds, then shut, before the phone takes the keys.
    private void GlanceAtMap(double delta)
    {
        _nextMap -= delta;

        if (_nextMap > 0)
        {
            return;
        }

        Press("map");
        _mapOpen = !_mapOpen;
        _nextMap = _mapOpen ? 2.5 : 25;
    }

    // At a workbench: take the battery out, then put one in, then close.
    private void Workbench(double delta)
    {
        Button? remove = GetTree().GetFirstNodeInGroup(WorkbenchPanel.RemoveGroup) as Button;
        Button? insert = GetTree().GetFirstNodeInGroup(WorkbenchPanel.InsertGroup) as Button;
        Button? button = remove ?? insert;

        if (button == null || !button.IsVisibleInTree())
        {
            _benchSeenFor = 0;
            return;
        }

        _benchSeenFor += delta;

        if (_benchSeenFor < ReadDelay)
        {
            return;
        }

        _benchSeenFor = 0;

        if (_benchClicks < 2)
        {
            _benchClicks++;
            GD.Print("Bot: clicking " + button.Text);
            Click(button.GetGlobalRect().GetCenter());
        }
        else
        {
            _benchClicks = 0;
            Press("ui_cancel");
        }
    }

    // A key press as an input event, for what the game takes from events (menus, the
    // inventory, the phone) rather than from polled actions.
    private static void Press(string action)
    {
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = true });
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = false });
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
