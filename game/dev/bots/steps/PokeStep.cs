namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Ui;

/// <summary>
/// Pokes at whatever is open: clicks a visible button at random, types into a text field
/// now and then, closes a panel after a few clicks and opens another (the bag, skills,
/// friends, the map, or the thing in reach). Online, it pokes at the terminal's screen
/// instead. Never a button that quits, leaves for the main menu or deletes.
/// </summary>
public sealed class PokeStep : BotStep
{
    private static readonly string[] Opens = { "inventory", "skills", "social", "map" };
    private static readonly string[] Denied = { "Quit", "main menu", "Leave to", "Delete", "Go Offline" };
    // Ordinary lines, and lines a careless or hostile player types: markup, other
    // scripts and emoji, far too long, blank, format strings, quotes for a database.
    public static readonly string[] Lines =
    {
        "hello", "testing 123", "this is my plan", "lfg", "brb",
        "[color=red]red[/color] [b]bold[/b] [url]x[/url]",
        "Åsa Ñoño 日本語 Привет 🙂🔥",
        new string('W', 400),
        new string('W', 119) + "🙂 past the cut",
        "   ",
        "%s %d {0} {{1}} \\n $name",
        "'; DROP TABLE players; --",
    };

    private readonly bool _terminal;
    private readonly double _seconds;
    private double _left;
    private double _next;
    private int _clicksHere;

    public PokeStep(bool terminal, double seconds)
        : base(terminal ? "poke at the terminal" : "poke around", seconds + 10)
    {
        _terminal = terminal;
        _seconds = seconds;
    }

    public override void Begin(BotBody body)
    {
        _left = _seconds;
        _next = 0;
        _clicksHere = 0;
        body.Stop();
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        _left -= delta;

        if (_left <= 0)
        {
            return StepResult.Done;
        }

        if (_terminal && !body.IsOnline)
        {
            return StepResult.Failed;
        }

        _next -= delta;

        if (_next > 0)
        {
            return StepResult.Running;
        }

        _next = (0.8 + body.Random.NextDouble()) * body.Pace;
        Node? root = _terminal ? body.OnlineScreen() : body.OpenPanels().Find(p => !(p is InGameMenu) && !(p is SettingsPanel));

        if (root == null)
        {
            Open(body);
            return StepResult.Running;
        }

        if (_clicksHere >= 3 + body.Random.Next(4) && !_terminal)
        {
            _clicksHere = 0;
            body.CloseOne();
            return StepResult.Running;
        }

        List<LineEdit> fields = body.OnScreen<LineEdit>(root);

        if (fields.Count > 0 && body.Random.Next(4) == 0)
        {
            LineEdit field = fields[body.Random.Next(fields.Count)];
            string line = Lines[body.Random.Next(Lines.Length)];
            GD.Print("Bot: typing \"" + line + "\" into " + field.Name);
            BotDriver.Click(field.GetGlobalRect().GetCenter());
            BotDriver.Type(line);
            _clicksHere++;
            return StepResult.Running;
        }

        List<BaseButton> buttons = body.OnScreen<BaseButton>(root);
        buttons.RemoveAll(IsDenied);

        if (buttons.Count == 0)
        {
            body.CloseOne();
            return StepResult.Running;
        }

        BaseButton pick = buttons[body.Random.Next(buttons.Count)];
        GD.Print("Bot: poking " + Label(pick) + " in " + root.Name);
        BotDriver.Click(pick.GetGlobalRect().GetCenter());
        _clicksHere++;
        return StepResult.Running;
    }

    private static void Open(BotBody body)
    {
        if (body.Prompt.Length > 0 && !body.Prompt.Contains("Go Online") && body.Random.Next(3) == 0)
        {
            GD.Print("Bot: poking at \"" + body.Prompt + "\"");
            body.Interact();
            return;
        }

        string key = Opens[body.Random.Next(Opens.Length)];
        GD.Print("Bot: opening " + key);
        BotBody.Press(key);
    }

    private static bool IsDenied(BaseButton button)
    {
        string label = Label(button);

        foreach (string denied in Denied)
        {
            if (label.Contains(denied, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string Label(BaseButton button)
    {
        Button? plain = button as Button;
        return plain != null && plain.Text.Length > 0 ? "\"" + plain.Text + "\"" : button.Name.ToString();
    }
}
