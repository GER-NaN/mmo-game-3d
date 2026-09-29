namespace MmoGame3d.Bots;

using System;

/// <summary>
/// Runs a piece of code once, in the world, for what no other step does: note a position,
/// write an event. It must not act on the game; acting is for the steps that go through
/// input.
/// </summary>
public class DoStep : BotStep
{
    private readonly Action<BotBody> _action;

    public DoStep(string what, Action<BotBody> action)
        : base(what, DefaultTimeLimit)
    {
        _action = action;
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        // Between zones (a party member's door) there is no body to read yet.
        if (body.Player == null)
        {
            return BotStepState.Running;
        }

        _action(body);
        return BotStepState.Done;
    }
}
