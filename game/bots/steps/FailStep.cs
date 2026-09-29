namespace MmoGame3d.Bots;

/// <summary>
/// Fails at once, saying why: a plan that could not be built as written (a chain naming
/// an activity that does not exist).
/// </summary>
public class FailStep : BotStep
{
    private readonly string _why;

    public FailStep(string why)
        : base("fail", DefaultTimeLimit)
    {
        _why = why;
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        return Fail(_why);
    }
}
