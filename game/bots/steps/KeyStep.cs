namespace MmoGame3d.Bots;

using Godot;

/// <summary>
/// Presses a key once, down on one frame and up on a later one, as a tap does. Down and
/// up in one frame would never be seen as "just pressed" by code that polls the key.
/// </summary>
public class KeyStep : BotStep
{
    private readonly string _action;
    private ulong _pressedOnFrame;

    public KeyStep(string action)
        : base("press " + action, DefaultTimeLimit)
    {
        _action = action;
    }

    public override BotIntent Intent
    {
        get { return BotIntent.Screen; }
    }

    public override void Start(BotBody body)
    {
        body.Key(_action, true);
        _pressedOnFrame = Engine.GetProcessFrames();
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        if (Engine.GetProcessFrames() == _pressedOnFrame)
        {
            return BotStepState.Running;
        }

        body.Key(_action, false);
        return BotStepState.Done;
    }
}
