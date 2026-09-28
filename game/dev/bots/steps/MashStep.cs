namespace MmoGame3d.Dev;
using Godot;

/// <summary>
/// Presses game keys at random, several a second: walking, jumping, using, the phone, the
/// bag, the map, Esc and Enter. Keys only, never a click, so it cannot press Quit; the
/// keeper closes a game menu it leaves open.
/// </summary>
public sealed class MashStep : BotStep
{
    private static readonly string[] Taps = { "interact", "phone", "inventory", "map", "skills", "social", "emp", "ui_cancel", "chat", "ui_accept" };
    private static readonly string[] Holds = { "move_forward", "move_back", "turn_left", "turn_right", "strafe_left", "strafe_right", "jump" };

    private readonly double _seconds;
    private double _left;
    private double _next;

    public MashStep(double seconds)
        : base("mash the keys", seconds + 5)
    {
        _seconds = seconds;
    }

    public override void Begin(BotBody body)
    {
        _left = _seconds;
        _next = 0;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        _left -= delta;

        if (_left <= 0)
        {
            body.Stop();
            return StepResult.Done;
        }

        _next -= delta;

        if (_next > 0)
        {
            return StepResult.Running;
        }

        _next = 0.1 + (body.Random.NextDouble() * 0.3);

        if (body.Random.Next(3) == 0)
        {
            string key = Taps[body.Random.Next(Taps.Length)];
            GD.Print("Bot: mashing " + key);
            BotBody.Press(key);
        }
        else
        {
            string key = Holds[body.Random.Next(Holds.Length)];

            if (Input.IsActionPressed(key))
            {
                Input.ActionRelease(key);
            }
            else
            {
                Input.ActionPress(key);
            }
        }

        return StepResult.Running;
    }
}
