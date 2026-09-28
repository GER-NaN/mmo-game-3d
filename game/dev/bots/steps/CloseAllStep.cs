namespace MmoGame3d.Dev;
/// <summary>Closes whatever is open, one thing a moment, until nothing is.</summary>
public sealed class CloseAllStep : BotStep
{
    private const double Gap = 0.5;
    private double _next;

    public CloseAllStep()
        : base("close everything", 10)
    {
    }

    public override void Begin(BotBody body)
    {
        _next = 0;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        _next -= delta;

        if (_next > 0)
        {
            return StepResult.Running;
        }

        _next = Gap;
        return body.CloseOne() ? StepResult.Done : StepResult.Running;
    }
}
