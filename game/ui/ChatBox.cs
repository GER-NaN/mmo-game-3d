namespace MmoGame3d.Ui;

using System;
using Godot;
using MmoGame3d.Rules.Chat;

/// <summary>
/// The chat history and, while typing, the line being written. Lines are added as
/// plain text, never as markup, so no player can inject formatting into another
/// player's screen. While the input has focus, the keys type instead of walking.
/// </summary>
public partial class ChatBox : VBoxContainer
{
    private const int MaxLines = 100;

    private static readonly Color NameColor = new Color(0.55f, 0.8f, 1f);
    private static readonly Color PartyColor = new Color(0.5f, 1f, 0.6f);
    private static readonly Color SystemColor = new Color(1f, 0.85f, 0.45f);

    private RichTextLabel _history = null!;
    private LineEdit _input = null!;
    private int _lines;

    public event Action<string>? Submitted;

    public bool IsTyping
    {
        get { return _input.Visible; }
    }

    public override void _Ready()
    {
        _history = GetNode<RichTextLabel>("%History");
        _input = GetNode<LineEdit>("%Input");
        _input.TextSubmitted += OnSubmitted;
        _input.GuiInput += OnInputGui;
    }

    public void Open()
    {
        _input.Visible = true;
        _input.GrabFocus();
    }

    public void AddLine(string sender, string text, ChatKind kind)
    {
        if (_lines >= MaxLines)
        {
            _history.RemoveParagraph(0);
        }
        else
        {
            _lines++;
        }

        if (_lines > 1)
        {
            _history.Newline();
        }

        switch (kind)
        {
            case ChatKind.System:
                _history.PushColor(SystemColor);
                _history.AddText(text);
                _history.Pop();
                break;
            case ChatKind.Party:
                _history.PushColor(PartyColor);
                _history.AddText("[Party] " + sender + ": " + text);
                _history.Pop();
                break;
            default:
                _history.PushColor(NameColor);
                _history.AddText(sender + ": ");
                _history.Pop();
                _history.AddText(text);
                break;
        }
    }

    private void OnSubmitted(string text)
    {
        if (text.Trim().Length > 0)
        {
            Submitted?.Invoke(text);
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
}
