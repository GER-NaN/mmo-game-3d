namespace MmoGame3d.Bots;

/// <summary>
/// Presses the game's keys fast and in any order for a while (the masher, bots.md R3):
/// input the game did not plan for. Whatever it leaves open, the keeper closes before the
/// next activity. It never presses the keys that leave the game.
/// </summary>
public class MashStep : BotStep
{
    private const double MashSeconds = 10;
    private const double PressEvery = 0.12;

    private static readonly string[] Keys =
    {
        "move_forward", "move_back", "turn_left", "turn_right", "strafe_left", "strafe_right",
        "jump", "interact", "phone", "inventory", "map", "social", "skills", "emp", "ui_cancel",
    };

    private double _mashed;
    private double _sincePress;
    private string _down = "";

    public MashStep()
        : base("mash the keys", MashSeconds + DefaultTimeLimit)
    {
    }

    public override BotIntent Intent
    {
        get { return BotIntent.Screen; }
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        _mashed += delta;
        _sincePress += delta;

        if (_sincePress < PressEvery)
        {
            return BotStepState.Running;
        }

        _sincePress = 0;

        if (_down.Length > 0)
        {
            body.Key(_down, false);
            _down = "";
        }

        if (_mashed >= MashSeconds)
        {
            return BotStepState.Done;
        }

        _down = Keys[body.Random.Next(Keys.Length)];
        body.Key(_down, true);
        return BotStepState.Running;
    }

    public override void End(BotBody body)
    {
        if (_down.Length > 0)
        {
            body.Key(_down, false);
        }
    }
}
