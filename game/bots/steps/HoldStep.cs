namespace MmoGame3d.Bots;

/// <summary>
/// Holds a key down for some seconds: walk forward for three seconds.
/// </summary>
public class HoldStep : BotStep
{
    private readonly string _action;
    private readonly double _seconds;
    private double _held;

    public HoldStep(string action, double seconds)
        : base("hold " + action + " " + seconds + " s", seconds + DefaultTimeLimit)
    {
        _action = action;
        _seconds = seconds;
    }

    public override void Start(BotBody body)
    {
        body.Hold(_action, true);
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        _held += delta;
        return _held >= _seconds ? BotStepState.Done : BotStepState.Running;
    }

    public override void End(BotBody body)
    {
        body.Hold(_action, false);
    }
}
