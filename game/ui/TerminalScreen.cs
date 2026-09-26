namespace MmoGame3d.Ui;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Client;
using MmoGame3d.Rules.Chat;
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
            input.TextSubmitted += text =>
            {
                CrackGuessSubmitted?.Invoke(text);
                input.Text = "";
            };
            content.AddChild(input);
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

        view.AddText(line.Kind == ChatKind.System ? line.Text : (line.Kind == ChatKind.Party ? "[Party] " : "") + line.Sender + ": " + line.Text);
    }

    private static void AddLine(VBoxContainer content, string text, Color color, int size)
    {
        Label label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", size);
        content.AddChild(label);
    }
}
