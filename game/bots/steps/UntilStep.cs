namespace MmoGame3d.Bots;

using System;

/// <summary>
/// Waits until a check on the client holds: the window is fullscreen, the phone is equipped.
/// Checked every frame, so a short-lived state (the top of a jump) is not missed.
/// </summary>
public class UntilStep : BotStep
{
    private readonly Func<BotBody, bool> _check;

    public UntilStep(string what, Func<BotBody, bool> check, double timeLimit)
        : base("until " + what, timeLimit)
    {
        _check = check;
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        return _check(body) ? BotStepState.Done : BotStepState.Running;
    }
}
