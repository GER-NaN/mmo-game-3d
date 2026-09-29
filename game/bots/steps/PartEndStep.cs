namespace MmoGame3d.Bots;

/// <summary>
/// Marks where an activity run inside another ends (a chain's Then, a need's provider),
/// so a StopIf in it skips only to here and the outer plan goes on.
/// </summary>
public class PartEndStep : BotStep
{
    public PartEndStep(string activity)
        : base("end of " + activity, DefaultTimeLimit)
    {
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        return BotStepState.Done;
    }
}
