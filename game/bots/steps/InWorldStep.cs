namespace MmoGame3d.Bots;

/// <summary>
/// Waits until the player is in the world: its body is there and a zone is loaded.
/// Connecting is the client's own (--autoconnect), so this may take a while.
/// </summary>
public class InWorldStep : BotStep
{
    private const double LookInterval = 0.2;

    private double _sinceLook = LookInterval;

    public InWorldStep()
        : base("in world", 30)
    {
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        _sinceLook += delta;

        if (_sinceLook < LookInterval)
        {
            return BotStepState.Running;
        }

        _sinceLook = 0;

        if (body.Player == null || body.Zone == null)
        {
            return BotStepState.Running;
        }

        body.Events.Write("in-world", body.Zone.ZoneId);
        return BotStepState.Done;
    }
}
