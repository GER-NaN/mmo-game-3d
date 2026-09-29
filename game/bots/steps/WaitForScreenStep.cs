namespace MmoGame3d.Bots;

using Godot;

/// <summary>
/// Waits until a screen of this type is visible: the main menu, the inventory.
/// </summary>
public class WaitForScreenStep<T> : BotStep
    where T : Control
{
    // Finding a screen walks the whole tree, so not every frame.
    private const double LookInterval = 0.2;

    private double _sinceLook = LookInterval;

    public WaitForScreenStep()
        : base("wait for " + typeof(T).Name, DefaultTimeLimit)
    {
    }

    public override BotIntent Intent
    {
        get { return BotIntent.Screen; }
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        _sinceLook += delta;

        if (_sinceLook < LookInterval)
        {
            return BotStepState.Running;
        }

        _sinceLook = 0;
        return body.Find<T>() != null ? BotStepState.Done : BotStepState.Running;
    }
}
