namespace MmoGame3d.Bots;

using System;

/// <summary>
/// Ends the activity here, as completed, when there is nothing for it to do (the lights
/// already work, the taxis are out of service); otherwise it goes on.
/// </summary>
public class StopIfStep : BotStep
{
    private readonly string _why;
    private readonly Func<BotBody, bool> _check;

    public StopIfStep(string why, Func<BotBody, bool> check)
        : base("stop if " + why, DefaultTimeLimit)
    {
        _why = why;
        _check = check;
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        if (_check(body))
        {
            body.Events.Write("nothing-to-do", _why);
            body.Run!.SkipRest();
        }

        return BotStepState.Done;
    }
}
