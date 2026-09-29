namespace MmoGame3d.Bots;

using System;
using Godot;
using MmoGame3d.Players;

/// <summary>
/// Now and then, a line in chat from the activity's phrases, typed as a player types it:
/// the chat key, each character as a key, then Enter. Only in the world, and only when
/// nothing else has the keys (no text field, no full screen), so it never types into a
/// screen the plan is working. Typing takes the keys for a few frames, as it does for a
/// player: a walk pauses while the line is written.
/// </summary>
public class BotChatter
{
    // Placeholders: seconds before the first line, and between lines.
    private const double FirstMin = 15;
    private const double FirstMax = 30;
    private const double BetweenMin = 45;
    private const double BetweenMax = 90;

    private readonly string[] _phrases;
    private readonly Random _random = new Random();
    private double _untilNext;
    private string _line = "";
    private int _stage;

    public BotChatter(string[] phrases)
    {
        _phrases = phrases;
        _untilNext = Between(FirstMin, FirstMax);
    }

    public void Tick(BotBody body, Node node, double delta)
    {
        if (_phrases.Length == 0)
        {
            return;
        }

        switch (_stage)
        {
            case 0:
                _untilNext -= delta;

                if (_untilNext > 0 || body.Player == null || !KeysFree(node))
                {
                    return;
                }

                _line = _phrases[_random.Next(_phrases.Length)];
                body.Key("chat", true);
                _stage = 1;
                break;
            case 1:
                body.Key("chat", false);
                _stage = 2;
                break;
            case 2:
                // The chat line takes the keys once it opens; if it did not, try later.
                LineEdit? field = node.GetViewport().GuiGetFocusOwner() as LineEdit;

                if (field == null)
                {
                    Rest();
                    return;
                }

                foreach (char letter in _line)
                {
                    Type(letter);
                }

                Enter();
                body.Events.Write("said", _line);
                Rest();
                break;
        }
    }

    private static bool KeysFree(Node node)
    {
        return node.GetViewport().GuiGetFocusOwner() == null && node.GetTree().GetNodeCountInGroup(ChaseCamera.ScreenGroup) == 0;
    }

    private static void Type(char letter)
    {
        InputEventKey key = new InputEventKey();
        key.Unicode = letter;
        key.Pressed = true;
        Input.ParseInputEvent(key);
    }

    private static void Enter()
    {
        InputEventKey key = new InputEventKey();
        key.Keycode = Key.Enter;
        key.PhysicalKeycode = Key.Enter;
        key.Pressed = true;
        Input.ParseInputEvent(key);

        InputEventKey up = (InputEventKey)key.Duplicate();
        up.Pressed = false;
        Input.ParseInputEvent(up);
    }

    private void Rest()
    {
        _stage = 0;
        _untilNext = Between(BetweenMin, BetweenMax);
    }

    private double Between(double min, double max)
    {
        return min + (_random.NextDouble() * (max - min));
    }
}
