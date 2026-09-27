namespace MmoGame3d.Ui;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Rules.Chat;
using MmoGame3d.Rules.Social;

/// <summary>
/// The chat: the one place to talk (world.md). "All" shows every line; each private
/// conversation gets a tab of its own, and typing in that tab writes to that player.
/// Lines are added as plain text, never as markup, so no player can inject formatting
/// into another player's screen. While the input has focus, the keys type instead of
/// walking.
/// </summary>
public partial class ChatBox : VBoxContainer
{
    private const int MaxLines = 100;
    private const string AllTab = "";

    private static readonly Color NameColor = new Color(0.55f, 0.8f, 1f);
    private static readonly Color PartyColor = new Color(0.5f, 1f, 0.6f);
    private static readonly Color SystemColor = new Color(1f, 0.85f, 0.45f);
    private static readonly Color DirectColor = new Color(1f, 0.6f, 0.9f);

    private readonly List<Entry> _entries = new List<Entry>();
    private readonly Dictionary<string, Button> _tabs = new Dictionary<string, Button>();
    private readonly Dictionary<string, string> _partnerNames = new Dictionary<string, string>();

    private RichTextLabel _history = null!;
    private LineEdit _input = null!;
    private HBoxContainer _tabRow = null!;

    // "" for All, or the player id of the conversation shown.
    private string _shown = AllTab;

    // To everyone (commands included).
    public event Action<string>? Submitted;

    // (player id, text): a private message.
    public event Action<string, string>? DirectSubmitted;

    public bool IsTyping
    {
        get { return _input.Visible; }
    }

    public override void _Ready()
    {
        _history = GetNode<RichTextLabel>("%History");
        _input = GetNode<LineEdit>("%Input");
        _tabRow = GetNode<HBoxContainer>("%Tabs");
        _input.TextSubmitted += OnSubmitted;
        _input.GuiInput += OnInputGui;
        AddTab(AllTab, "All");
        ShowTab(AllTab);
    }

    public void Open()
    {
        _input.PlaceholderText = _shown == AllTab
            ? "Say something. /p party, /wave /cheer /sit /pushups. Enter sends."
            : "To " + _partnerNames[_shown] + ". /wave /cheer /sit /pushups. Enter sends.";
        _input.Visible = true;
        _input.GrabFocus();
    }

    // Opens (or makes) the conversation with a player and starts typing to them.
    public void OpenDirect(string playerId, string name)
    {
        _partnerNames[playerId] = name;
        AddTab(playerId, name);
        ShowTab(playerId);
        Open();
    }

    public void AddLine(string sender, string text, ChatKind kind)
    {
        Add(new Entry(sender, text, kind, "", false));
    }

    // A private line, sent or received; partner is the other player.
    public void AddDirect(string partnerId, string partnerName, string text, bool incoming)
    {
        _partnerNames[partnerId] = partnerName;
        AddTab(partnerId, partnerName);
        Add(new Entry(partnerName, text, ChatKind.Direct, partnerId, incoming));

        if (incoming && _shown != partnerId)
        {
            _tabs[partnerId].Text = "@" + partnerName + " *";
        }
    }

    private void Add(Entry entry)
    {
        _entries.Add(entry);

        if (_entries.Count > MaxLines * 4)
        {
            _entries.RemoveAt(0);
        }

        if (_shown == AllTab || _shown == entry.PartnerId)
        {
            Write(entry);
        }
    }

    private void AddTab(string key, string name)
    {
        if (_tabs.ContainsKey(key))
        {
            return;
        }

        Button tab = new Button { Text = key == AllTab ? name : "@" + name, FocusMode = FocusModeEnum.None, Flat = true };
        tab.AddThemeFontSizeOverride("font_size", 14);
        tab.Pressed += () => ShowTab(key);
        _tabRow.AddChild(tab);
        _tabs[key] = tab;
    }

    private void ShowTab(string key)
    {
        _shown = key;
        _history.Clear();
        int matching = 0;

        foreach (KeyValuePair<string, Button> tab in _tabs)
        {
            tab.Value.Modulate = tab.Key == key ? Colors.White : new Color(1f, 1f, 1f, 0.55f);

            if (tab.Key == key && key != AllTab)
            {
                tab.Value.Text = "@" + _partnerNames[key];
            }
        }

        foreach (Entry entry in _entries)
        {
            if (key == AllTab || entry.PartnerId == key)
            {
                matching++;
            }
        }

        int skip = Math.Max(0, matching - MaxLines);

        foreach (Entry entry in _entries)
        {
            if (key != AllTab && entry.PartnerId != key)
            {
                continue;
            }

            if (skip > 0)
            {
                skip--;
                continue;
            }

            Write(entry);
        }

        if (_input.Visible)
        {
            Open();
        }
    }

    private void Write(Entry entry)
    {
        if (_history.GetParagraphCount() > 1 || _history.GetParsedText().Length > 0)
        {
            _history.Newline();
        }

        switch (entry.Kind)
        {
            case ChatKind.System:
                _history.PushColor(SystemColor);
                _history.AddText(entry.Text);
                _history.Pop();
                break;
            case ChatKind.Party:
                _history.PushColor(PartyColor);
                _history.AddText("[Party] " + entry.Sender + ": " + entry.Text);
                _history.Pop();
                break;
            case ChatKind.Direct:
                _history.PushColor(DirectColor);
                _history.AddText((entry.Incoming ? "[From " : "[To ") + entry.Sender + "] " + entry.Text);
                _history.Pop();
                break;
            default:
                _history.PushColor(NameColor);
                _history.AddText(entry.Sender + ": ");
                _history.Pop();
                _history.AddText(entry.Text);
                break;
        }
    }

    private void OnSubmitted(string text)
    {
        if (text.Trim().Length > 0)
        {
            // A command works on any tab: an emote plays, "/p " goes to the party.
            bool command = Gestures.EmoteIn(text) != null || text.StartsWith("/p ", StringComparison.OrdinalIgnoreCase);

            if (_shown == AllTab || command)
            {
                Submitted?.Invoke(text);
            }
            else
            {
                DirectSubmitted?.Invoke(_shown, text);
            }
        }

        Close();
    }

    // Esc closes the line here, so it does not also open the game menu.
    private void OnInputGui(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            Close();
            _input.AcceptEvent();
        }
    }

    private void Close()
    {
        _input.Text = "";
        _input.Visible = false;
        _input.ReleaseFocus();
    }

    private class Entry
    {
        public Entry(string sender, string text, ChatKind kind, string partnerId, bool incoming)
        {
            Sender = sender;
            Text = text;
            Kind = kind;
            PartnerId = partnerId;
            Incoming = incoming;
        }

        public string Sender { get; }
        public string Text { get; }
        public ChatKind Kind { get; }
        public string PartnerId { get; }
        public bool Incoming { get; }
    }
}
