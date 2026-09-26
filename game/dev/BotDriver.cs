namespace MmoGame3d.Dev;

using System;
using Godot;

/// <summary>
/// Plays a client by itself, for load tests and headless checks. It presses the same
/// input actions a person presses, so everything past the keyboard is the real game:
/// walks with pauses, turns now and then, and jumps sometimes.
/// </summary>
public partial class BotDriver : Node
{
    // Placeholders for a wanderer; seconds.
    private const double MinSpell = 1.0;
    private const double MaxSpell = 4.0;

    private readonly Random _random = new Random();
    private double _spellLeft;

    public override void _Process(double delta)
    {
        _spellLeft -= delta;

        if (_spellLeft > 0)
        {
            return;
        }

        _spellLeft = MinSpell + (_random.NextDouble() * (MaxSpell - MinSpell));
        Release();

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

    public override void _ExitTree()
    {
        Release();
    }

    private static void Release()
    {
        Input.ActionRelease("move_forward");
        Input.ActionRelease("turn_left");
        Input.ActionRelease("turn_right");
        Input.ActionRelease("jump");
    }
}
