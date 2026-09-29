namespace MmoGame3d.Bots;

using System;

/// <summary>
/// Runs a piece of code once, for what no other step does: note a position, write an
/// event. It must not act on the game; acting is for the steps that go through input.
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
        _action(body);
        return BotStepState.Done;
    }
}
