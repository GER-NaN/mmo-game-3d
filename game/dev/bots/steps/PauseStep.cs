namespace MmoGame3d.Dev;
/// <summary>Stands still for a moment, as a person does to read.</summary>
public sealed class PauseStep : BotStep
{
    private readonly double _seconds;
    private double _left;

    public PauseStep(double seconds)
        : base("pause", (seconds * 4) + 1)
    {
        _seconds = seconds;
    }

    public override void Begin(BotBody body)
    {
        _left = _seconds * body.Pace;
        body.Stop();
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        _left -= delta;
        return _left <= 0 ? StepResult.Done : StepResult.Running;
    }
}
