namespace MmoGame3d.Ui;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Client;
using MmoGame3d.Rules.Chat;
using MmoGame3d.Rules.Skills;
using MmoGame3d.Rules.Social;
using MmoGame3d.Rules.Terminals;
using MmoGame3d.Rules.Town;

/// <summary>
/// The terminal OS: a full-screen space with its own look, the app list on the left and
/// the open app on the right. Every app shows, locked ones with their notice, so a
/// player sees the whole game from here. The look is a mock (world.md: the production
/// look comes later without rebuilding what is underneath).
/// </summary>
public partial class TerminalScreen : Control
{
    // Bots find the Go Offline button by this group, then click it like a person.
    public const string GoOfflineGroup = "terminal_go_offline";

    private static readonly Color Text = new Color(0.55f, 1f, 0.7f);
    private static readonly Color Dim = new Color(0.55f, 1f, 0.7f, 0.5f);
    private static readonly Color Locked = new Color(1f, 0.8f, 0.4f);

    private readonly List<ChatLine> _chat = new List<ChatLine>();
    private string[] _rosterNames = Array.Empty<string>();
    private string[] _rosterZones = Array.Empty<string>();
    private int[] _rosterOnline = Array.Empty<int>();
    private string _openApp = TerminalApps.Chat;
    private RichTextLabel? _chatView;

    // Bots find the Take the job button by this group, then click it like a person.
    public const string TakeJobGroup = "terminal_take_job";

    private bool _lightsWorking;
    private bool _jobTaken;
    private string[] _townLog = Array.Empty<string>();

    public event Action? GoOfflinePressed;
    public event Action<string>? ChatSubmitted;
    public event Action? TakeJobPressed;
    public event Action? CrackStartPressed;

    // Whois: search text, open a page by player id, your own page, props, your own
    // settings (plan, show skills, show location), add friend, message (id, name).
    public event Action<string>? WhoisSearchSubmitted;
    public event Action<string>? WhoisOpenPressed;
    public event Action? WhoisMinePressed;
    public event Action<string>? WhoisPropsPressed;
    public event Action<string, bool, bool>? WhoisEditSubmitted;
    public event Action<string>? WhoisFriendPressed;
    public event Action<string, string>? WhoisMessagePressed;

    // Bots find Whois's parts by these groups.
    public const string WhoisMineGroup = "whois_mine";
    public const string WhoisPropsGroup = "whois_props";

    // What Whois shows: the last results, or a page (null for none).
    private string[] _whoisIds = Array.Empty<string>();
    private string[] _whoisNames = Array.Empty<string>();
    private string[] _whoisTitles = Array.Empty<string>();
    private int[] _whoisLevels = Array.Empty<int>();
    private int[] _whoisOnline = Array.Empty<int>();
    private Godot.Collections.Dictionary? _whoisPage;
    public event Action<string>? CrackGuessSubmitted;

    // Bots find the code cracker's parts by these groups.
    public const string CrackStartGroup = "terminal_crack_start";
    public const string CrackInputGroup = "terminal_crack_input";

    // Each app's button is in the group AppGroupPrefix + its id, for bots.
    public const string AppGroupPrefix = "terminal_app_";

    // What the screen shows of the code cracker, read by bots as a person reads it.
    public string[]? CrackGuesses
    {
        get { return _crackGuesses; }
    }

    public int[] CrackExact
    {
        get { return _crackExact; }
    }

    public int[] CrackPartial
    {
        get { return _crackPartial; }
    }

    public int CrackStatus
    {
        get { return _crackStatus; }
    }

    // The code cracker as the server last told it; null before the first code.
    private string[]? _crackGuesses;
    private int[] _crackExact = Array.Empty<int>();
    private int[] _crackPartial = Array.Empty<int>();
    private string[] _crackPositions = Array.Empty<string>();
    private int _crackLeft;
    private int _crackStatus;

    public override void _Ready()
    {
        AddToGroup(Players.ChaseCamera.ScreenGroup);

        // A terminal face: monospace where the system has one.
        SystemFont font = new SystemFont { FontNames = new[] { "Consolas", "Cascadia Mono", "DejaVu Sans Mono", "monospace" } };
        Theme = new Theme { DefaultFont = font, DefaultFontSize = 17 };

        Button goOffline = GetNode<Button>("%GoOffline");
        goOffline.AddToGroup(GoOfflineGroup);
        goOffline.Pressed += () => GoOfflinePressed?.Invoke();
    }

    public void Open(TerminalType door, string terminalName, IReadOnlyList<ChatLine> chatSoFar)
    {
        bool phone = door == TerminalType.Phone;
        Label title = GetNode<Label>("%Title");
        title.Text = phone ? "TownOS" : "TownOS  //  " + door + " terminal  //  " + terminalName;
        title.AddThemeColorOverride("font_color", Text);

        _chat.AddRange(chatSoFar);

        if (phone)
        {
            ShrinkToPhone();
        }

        VBoxContainer apps = GetNode<VBoxContainer>("%Apps");

        foreach (TerminalApp app in TerminalApps.For(door))
        {
            Button button = new Button
            {
                Text = (app.State == AppState.Locked ? "[locked] " : "") + app.Name,
                Alignment = HorizontalAlignment.Left,
                FocusMode = FocusModeEnum.None,

                // On a phone the column is narrow: long names are cut, not widening it.
                ClipText = phone,
                TooltipText = app.Name,
            };
            button.AddThemeColorOverride("font_color", app.State == AppState.Locked ? Locked : Text);
            TerminalApp chosen = app;
            button.Pressed += () => ShowApp(chosen);
            button.AddToGroup(AppGroupPrefix + app.Id);
            apps.AddChild(button);
        }

        ShowApp(TerminalApps.For(door)[0]);
    }

    // world.md: the phone does not take the whole screen. It is held up in the middle,
    // and the world goes on around it, seen but not reachable. The size is a placeholder.
    private void ShrinkToPhone()
    {
        Vector2 half = new Vector2(210f, 320f);

        foreach (string part in new[] { "Background", "Margin" })
        {
            Control control = GetNode<Control>(part);
            control.SetAnchorsPreset(LayoutPreset.Center);
            control.OffsetLeft = -half.X;
            control.OffsetRight = half.X;
            control.OffsetTop = -half.Y;
            control.OffsetBottom = half.Y;
        }

        GetNode<Control>("Margin").AddThemeConstantOverride("margin_left", 16);
        GetNode<Control>("Margin").AddThemeConstantOverride("margin_right", 16);
        GetNode<Control>("%Apps").CustomMinimumSize = new Vector2(130f, 0f);
        GetNode<Control>("%Apps").SizeFlagsStretchRatio = 0.6f;
        GetNode<Control>("%Apps").SizeFlagsHorizontal = SizeFlags.ExpandFill;

        // The world around the phone takes no clicks while you are on it.
        MouseFilter = MouseFilterEnum.Stop;
    }

    public void AddChatLine(ChatLine line)
    {
        _chat.Add(line);

        if (_chatView != null)
        {
            AppendChat(_chatView, line);
        }
    }

    public void ShowTown(bool lightsWorking, bool jobTaken, string[] log)
    {
        _lightsWorking = lightsWorking;
        _jobTaken = jobTaken;
        _townLog = log;

        if (_openApp == TerminalApps.TodoList || _openApp == TerminalApps.TownLog)
        {
            ShowApp(new TerminalApp(_openApp, _openApp == TerminalApps.TodoList ? "Town repairs" : "Town log", ""));
        }
    }

    public void ShowCrack(string[] guesses, int[] exact, int[] partial, string[] positions, int left, int status)
    {
        _crackGuesses = guesses;
        _crackExact = exact;
        _crackPartial = partial;
        _crackPositions = positions;
        _crackLeft = left;
        _crackStatus = status;

        if (_openApp == TerminalApps.CodeCracker)
        {
            ShowApp(new TerminalApp(TerminalApps.CodeCracker, "Code cracker", ""));
        }
    }

    public void ShowWhoisResults(string[] ids, string[] names, string[] titles, int[] levels, int[] online)
    {
        _whoisIds = ids;
        _whoisNames = names;
        _whoisTitles = titles;
        _whoisLevels = levels;
        _whoisOnline = online;
        _whoisPage = null;
        RefreshWhois();
    }

    public void ShowWhoisPage(Godot.Collections.Dictionary page)
    {
        _whoisPage = page;
        RefreshWhois();
    }

    // Whois may be asked for from outside the app (a page opened for you): show it.
    private void RefreshWhois()
    {
        ShowApp(new TerminalApp(TerminalApps.Whois, "Whois", ""));
    }

    public void ShowRoster(string[] names, string[] zones, int[] online)
    {
        _rosterNames = names;
        _rosterZones = zones;
        _rosterOnline = online;

        if (_openApp == TerminalApps.Online)
        {
            ShowApp(new TerminalApp(TerminalApps.Online, "Who's online", ""));
        }
    }

    private void ShowApp(TerminalApp app)
    {
        VBoxContainer content = GetNode<VBoxContainer>("%Content");

        foreach (Node old in content.GetChildren())
        {
            old.QueueFree();
        }

        _openApp = app.Id;
        _chatView = null;
        AddLine(content, app.Name, Text, 20);

        if (app.State == AppState.Locked)
        {
            AddLine(content, app.LockedNotice, Locked, 17);
            return;
        }

        switch (app.Id)
        {
            case TerminalApps.Chat:
                ShowChat(content);
                break;
            case TerminalApps.Online:
                ShowOnline(content);
                break;
            case TerminalApps.TodoList:
                ShowRepairs(content);
                break;
            case TerminalApps.TownLog:
                if (_townLog.Length == 0)
                {
                    AddLine(content, "Nothing has been repaired here yet.", Text, 17);
                }

                foreach (string entry in _townLog)
                {
                    AddLine(content, entry, Text, 16);
                }

                break;
            case TerminalApps.CodeCracker:
                ShowCrack(content);
                break;
            case TerminalApps.Whois:
                ShowWhois(content);
                break;
            case TerminalApps.StatusBoard:
                AddLine(content, "Data centre raid in progress: Ashford", Text, 17);
                AddLine(content, "Power cut after a substation hack: Millbrook", Text, 17);
                AddLine(content, "Placeholder events until the world makes its own.", Dim, 15);
                break;
            case TerminalApps.ExchangeRate:
                AddLine(content, "1 GPU core = 14.20 credits", Text, 17);
                AddLine(content, "You have no wallet to trade with yet.", Dim, 15);
                break;
        }
    }

    private void ShowChat(VBoxContainer content)
    {
        RichTextLabel view = new RichTextLabel { SizeFlagsVertical = SizeFlags.ExpandFill, ScrollFollowing = true };
        view.AddThemeColorOverride("default_color", Text);
        content.AddChild(view);

        foreach (ChatLine line in _chat)
        {
            AppendChat(view, line);
        }

        LineEdit input = new LineEdit { PlaceholderText = "Type and press Enter", MaxLength = CleanFilter.MaxLength };
        input.TextSubmitted += text =>
        {
            if (text.Trim().Length > 0)
            {
                ChatSubmitted?.Invoke(text);
            }

            input.Text = "";
        };
        content.AddChild(input);
        _chatView = view;
    }

    // The town's TODO list: the one job there is, and what state it is in.
    private void ShowRepairs(VBoxContainer content)
    {
        if (_lightsWorking)
        {
            AddLine(content, "Nothing needs repairing right now.", Text, 17);
            AddLine(content, "The street lights on Main Street are working.", Dim, 15);
            return;
        }

        AddLine(content, StreetLights.JobTitle, Text, 18);
        AddLine(content, StreetLights.JobText, Dim, 15);

        if (_jobTaken)
        {
            AddLine(content, "You have this job. The junction box is on Main Street, west of the crossing.", Locked, 16);
            return;
        }

        Button take = new Button { Text = "Take the job", FocusMode = FocusModeEnum.None, SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
        take.AddToGroup(TakeJobGroup);
        take.Pressed += () => TakeJobPressed?.Invoke();
        content.AddChild(take);
    }

    // Whois: a search line and your own page on top; below, the results or one page.
    private void ShowWhois(VBoxContainer content)
    {
        HBoxContainer search = new HBoxContainer();
        LineEdit text = new LineEdit { PlaceholderText = "Search a name", SizeFlagsHorizontal = SizeFlags.ExpandFill, MaxLength = 24 };
        text.TextSubmitted += value => WhoisSearchSubmitted?.Invoke(value);
        search.AddChild(text);
        Button mine = new Button { Text = "My page", FocusMode = FocusModeEnum.None };
        mine.AddToGroup(WhoisMineGroup);
        mine.Pressed += () => WhoisMinePressed?.Invoke();
        search.AddChild(mine);
        content.AddChild(search);

        if (_whoisPage != null)
        {
            ShowWhoisPage(content, _whoisPage);
            return;
        }

        if (_whoisIds.Length == 0)
        {
            AddLine(content, "Type a name and press Enter. Names are not unique: the career and level tell players apart.", Dim, 15);
            return;
        }

        for (int i = 0; i < _whoisIds.Length; i++)
        {
            string id = _whoisIds[i];
            string title = _whoisTitles[i].Length > 0 ? _whoisTitles[i] : "No career";
            Button row = new Button
            {
                Text = (_whoisOnline[i] != 0 ? "● " : "○ ") + _whoisNames[i] + "   " + title + "   Level " + _whoisLevels[i],
                Alignment = HorizontalAlignment.Left,
                FocusMode = FocusModeEnum.None,
            };
            row.AddThemeColorOverride("font_color", _whoisOnline[i] != 0 ? Text : Dim);
            row.Pressed += () => WhoisOpenPressed?.Invoke(id);
            content.AddChild(row);
        }
    }

    private void ShowWhoisPage(VBoxContainer content, Godot.Collections.Dictionary page)
    {
        string id = (string)page["id"];
        string name = (string)page["name"];
        bool own = (bool)page["own"];
        bool online = (bool)page["online"];

        AddLine(content, name + "      Level " + (int)page["level"], Text, 20);
        string title = (string)page["title"];

        if (title.Length == 0)
        {
            AddLine(content, "No career", Dim, 16);
        }
        else
        {
            string next = (string)page["next"];
            float progress = (float)page["progress"];
            string bar = next.Length == 0 ? "" : "   [" + new string('#', (int)(progress * 16)) + new string('.', 16 - (int)(progress * 16)) + "]  " + (int)(progress * 100) + "% to " + next;
            AddLine(content, title + bar, Text, 16);
        }

        string where = (string)page["where"];
        string doing = (string)page["doing"];
        AddLine(content, (online ? "● " : "○ ") + (where.Length > 0 ? where : (online ? "Location hidden" : "Offline")), online ? Text : Dim, 16);

        if (doing.Length > 0)
        {
            AddLine(content, "   " + doing, Dim, 15);
        }

        AddLine(content, "plan", Dim, 14);
        string plan = (string)page["plan"];

        if (own)
        {
            LineEdit planEdit = new LineEdit { Text = plan, PlaceholderText = "Write your plan. Enter saves.", MaxLength = WhoisSettings.MaxPlanLength };
            CheckBox showSkills = new CheckBox { Text = "Show Skills", ButtonPressed = (bool)page["showSkills"], FocusMode = FocusModeEnum.None };
            CheckBox showLocation = new CheckBox { Text = "Show Location", ButtonPressed = (bool)page["showLocation"], FocusMode = FocusModeEnum.None };
            planEdit.TextSubmitted += value => WhoisEditSubmitted?.Invoke(value, showSkills.ButtonPressed, showLocation.ButtonPressed);
            showSkills.Toggled += on => WhoisEditSubmitted?.Invoke(planEdit.Text, on, showLocation.ButtonPressed);
            showLocation.Toggled += on => WhoisEditSubmitted?.Invoke(planEdit.Text, showSkills.ButtonPressed, on);
            content.AddChild(planEdit);
            HBoxContainer toggles = new HBoxContainer();
            toggles.AddChild(showSkills);
            toggles.AddChild(showLocation);
            content.AddChild(toggles);
        }
        else
        {
            AddLine(content, plan.Length > 0 ? "\"" + plan + "\"" : "No plan.", plan.Length > 0 ? Text : Dim, 16);
        }

        AddLine(content, "skills", Dim, 14);

        if ((bool)page["skillsShown"])
        {
            int[] skillIds = (int[])page["skillIds"];
            long[] skillXp = (long[])page["skillXp"];
            List<string> parts = new List<string>();

            for (int i = 0; i < skillIds.Length && i < skillXp.Length; i++)
            {
                if (SkillCatalog.IsKnown(skillIds[i]))
                {
                    parts.Add(SkillCatalog.Name((SkillId)skillIds[i]) + " " + SkillCatalog.LevelFor(skillXp[i]));
                }
            }

            AddLine(content, string.Join("    ", parts), Text, 16);

            if (own && !(bool)page["showSkills"])
            {
                AddLine(content, "Only you see these. Turn on Show Skills to show them.", Dim, 14);
            }
        }
        else
        {
            AddLine(content, "Skills are private.", Dim, 16);
        }

        AddLine(content, "^ " + (int)page["props"] + " props      " + (int)page["friends"] + " friends", Text, 16);

        if (own)
        {
            return;
        }

        HBoxContainer actions = new HBoxContainer();
        Button props = new Button { Text = (bool)page["gave"] ? "Take back props" : "Give props", FocusMode = FocusModeEnum.None };
        props.AddToGroup(WhoisPropsGroup);
        props.Pressed += () => WhoisPropsPressed?.Invoke(id);
        actions.AddChild(props);

        if (!(bool)page["isFriend"])
        {
            Button friend = new Button { Text = "Add friend", FocusMode = FocusModeEnum.None };
            friend.Pressed += () => WhoisFriendPressed?.Invoke(id);
            actions.AddChild(friend);
        }

        if (online)
        {
            Button message = new Button { Text = "Message", FocusMode = FocusModeEnum.None };
            message.Pressed += () => WhoisMessagePressed?.Invoke(id, name);
            actions.AddChild(message);
        }

        content.AddChild(actions);
    }

    // Four digits, 0 to 5, eight guesses. Each answer: right and in place, right but
    // elsewhere. The Hacking skill's practice ground.
    private void ShowCrack(VBoxContainer content)
    {
        AddLine(content, "Crack the 4-digit code (digits 0 to 5). After each guess: how many digits are in the right place, and how many are right but elsewhere.", Dim, 15);

        if (_crackGuesses != null)
        {
            for (int i = 0; i < _crackGuesses.Length && i < _crackExact.Length && i < _crackPartial.Length; i++)
            {
                string places = i < _crackPositions.Length && _crackPositions[i].Length > 0 ? "   " + _crackPositions[i] : "";
                AddLine(content, _crackGuesses[i] + "   in place " + _crackExact[i] + "   elsewhere " + _crackPartial[i] + places, Text, 17);
            }
        }

        if (_crackGuesses != null && _crackStatus == 0)
        {
            AddLine(content, _crackLeft + " guesses left", Dim, 15);
            LineEdit input = new LineEdit { PlaceholderText = "Guess, then Enter", MaxLength = 4 };
            input.AddToGroup(CrackInputGroup);
            Label problem = AddLine(content, "", Locked, 15);
            input.TextSubmitted += text =>
            {
                // Checked here too, so a slip keeps what was typed and says why.
                if (!IsCrackGuess(text))
                {
                    problem.Text = "Four digits, each 0 to 5.";
                    return;
                }

                CrackGuessSubmitted?.Invoke(text);
                input.Text = "";
            };
            content.AddChild(input);
            content.MoveChild(problem, content.GetChildCount() - 1);
            input.CallDeferred(Control.MethodName.GrabFocus);
            return;
        }

        if (_crackGuesses != null)
        {
            AddLine(content, _crackStatus == 1 ? "Cracked." : "Locked out. The code was not found.", _crackStatus == 1 ? Text : Locked, 17);
        }

        Button start = new Button { Text = "New code", FocusMode = FocusModeEnum.None, SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
        start.AddToGroup(CrackStartGroup);
        start.Pressed += () => CrackStartPressed?.Invoke();
        content.AddChild(start);
    }

    private static bool IsCrackGuess(string text)
    {
        if (text.Length != 4)
        {
            return false;
        }

        foreach (char digit in text)
        {
            if (digit < '0' || digit > '5')
            {
                return false;
            }
        }

        return true;
    }

    // The HUD's notices are under the terminal: a refusal shows here too.
    public void ShowNotice(string text)
    {
        HBoxContainer header = GetNode<HBoxContainer>("Margin/Rows/Header");
        Label? notice = header.GetNodeOrNull<Label>("Notice");

        if (notice == null)
        {
            notice = new Label { Name = "Notice" };
            notice.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.45f));
            header.AddChild(notice);
            header.MoveChild(notice, header.GetChildCount() - 2);
        }

        notice.Text = text;
        notice.Modulate = Colors.White;
        Tween tween = notice.CreateTween();
        tween.TweenInterval(4.0);
        tween.TweenProperty(notice, "modulate:a", 0f, 1.0);
    }

    private void ShowOnline(VBoxContainer content)
    {
        AddLine(content, _rosterNames.Length + " in the world", Dim, 15);

        for (int i = 0; i < _rosterNames.Length && i < _rosterZones.Length && i < _rosterOnline.Length; i++)
        {
            string status = _rosterOnline[i] != 0 ? "online" : "in " + _rosterZones[i];
            AddLine(content, _rosterNames[i] + "  -  " + status, _rosterOnline[i] != 0 ? Text : Dim, 17);
        }
    }

    // Plain text only, never markup: player names and lines cannot inject formatting.
    private static void AppendChat(RichTextLabel view, ChatLine line)
    {
        if (view.GetParagraphCount() > 1 || view.GetParsedText().Length > 0)
        {
            view.Newline();
        }

        string prefix = line.Kind == ChatKind.Party ? "[Party] " : "";
        view.AddText(line.Kind == ChatKind.System || line.Kind == ChatKind.Direct ? line.Text : prefix + line.Sender + ": " + line.Text);
    }

    private static Label AddLine(VBoxContainer content, string text, Color color, int size)
    {
        Label label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", size);
        content.AddChild(label);
        return label;
    }
}
