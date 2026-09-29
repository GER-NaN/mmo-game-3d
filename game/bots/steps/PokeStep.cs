namespace MmoGame3d.Bots;

using System.Collections.Generic;
using Godot;

/// <summary>
/// The curious player (bots.md R3): opens a screen on a key picked at random and clicks
/// buttons it finds on whatever is open, a few times over, to see what they do. It leaves
/// alone the buttons that leave the game or undo something for good. Whatever it leaves
/// open, the keeper closes before the next activity.
/// </summary>
public class PokeStep : BotStep
{
    private const int Clicks = 8;
    private const double Pause = 0.7;

    private static readonly string[] OpenKeys = { "inventory", "map", "social", "skills", "ui_cancel" };

    // Words on buttons it never presses.
    private static readonly string[] Never = { "Quit", "Leave", "Delete", "Log out", "Recycle", "Drop", "Give" };

    private double _sinceClick;
    private int _clicked;
    private bool _opened;

    public PokeStep()
        : base("poke around the screens", 60)
    {
    }

    public override BotIntent Intent
    {
        get { return BotIntent.Screen; }
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        _sinceClick += delta;

        if (_sinceClick < Pause)
        {
            return BotStepState.Running;
        }

        _sinceClick = 0;

        if (!_opened)
        {
            _opened = true;
            string key = OpenKeys[body.Random.Next(OpenKeys.Length)];
            body.Events.Write("poking", "opens with " + key);
            body.Key(key, true);
            body.Key(key, false);
            return BotStepState.Running;
        }

        if (_clicked >= Clicks)
        {
            return BotStepState.Done;
        }

        _clicked++;
        List<Button> buttons = new List<Button>();
        Node? ui = body.Player?.GetTree().Root.GetNodeOrNull("Main/ClientGame/Ui");
        Gather(ui, buttons);

        if (buttons.Count == 0)
        {
            return BotStepState.Running;
        }

        Button pick = buttons[body.Random.Next(buttons.Count)];
        body.Events.Write("poking", "clicks \"" + pick.Text + "\"");
        body.Click(pick);
        return BotStepState.Running;
    }

    // Every visible, enabled button a click could press, but the ones it never presses.
    private static void Gather(Node? node, List<Button> buttons)
    {
        if (node == null)
        {
            return;
        }

        foreach (Node child in node.GetChildren())
        {
            Button? button = child as Button;

            if (button != null && button.IsVisibleInTree() && !button.Disabled && !IsNever(button.Text))
            {
                buttons.Add(button);
            }

            Gather(child, buttons);
        }
    }

    private static bool IsNever(string text)
    {
        foreach (string word in Never)
        {
            if (text.Contains(word))
            {
                return true;
            }
        }

        return false;
    }
}
