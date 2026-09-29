namespace MmoGame3d.Bots;

using System.Collections.Generic;

/// <summary>
/// A prerequisite (bots.md F2a, R5): if the fact does not hold, the bot earns it by play.
/// The first activity in the registry that provides the fact runs next, then a check that
/// the fact now holds. If it already holds, nothing runs.
/// </summary>
public class NeedStep : BotStep
{
    private readonly BotFact _fact;

    public NeedStep(BotFact fact)
        : base("need " + fact.Name, DefaultTimeLimit)
    {
        _fact = fact;
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        if (_fact.Holds(body))
        {
            body.Events.Write("need", _fact.Name + ": holds");
            return BotStepState.Done;
        }

        BotActivity? provider = BotActivities.ProviderOf(_fact);

        if (provider == null)
        {
            return Fail("nothing provides " + _fact.Name);
        }

        body.Events.Write("need", _fact.Name + ": from \"" + provider.Name + "\"");
        List<BotStep> steps = provider.Steps();
        steps.Add(new UntilStep(_fact.Name, _fact.Holds, DefaultTimeLimit));
        body.InsertNext(steps);
        return BotStepState.Done;
    }
}
