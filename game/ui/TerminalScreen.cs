namespace MmoGame3d.Ui;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Client;
using MmoGame3d.Rules.Chat;
using MmoGame3d.Rules.Terminals;

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

    public event Action? GoOfflinePressed;
    public event Action<string>? ChatSubmitted;

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
                AddLine(content, "Street lights on Main Street are out.", Text, 17);
                AddLine(content, "The repair job opens in a later build.", Dim, 15);
                break;
            case TerminalApps.TownLog:
                AddLine(content, "Nothing has been repaired here yet.", Text, 17);
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
