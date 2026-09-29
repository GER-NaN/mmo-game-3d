namespace MmoGame3d.Bots;

/// <summary>
/// Does nothing for some seconds: a pause to settle, or a stay in a place.
/// </summary>
public class WaitStep : BotStep
{
    private readonly double _seconds;
    private double _waited;

    public WaitStep(double seconds)
        : base("wait " + seconds + " s", seconds + DefaultTimeLimit)
    {
        _seconds = seconds;
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        _waited += delta;
        return _waited >= _seconds ? BotStepState.Done : BotStepState.Running;
    }
}
