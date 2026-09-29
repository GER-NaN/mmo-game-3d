namespace MmoGame3d.Bots;

using System.Collections.Generic;

/// <summary>
/// A prerequisite (bots.md F2a, R5): if the fact does not hold, the bot earns it by play.
/// An activity that provides the fact's key, picked at random among those that do, runs
/// next, then this need again: some facts take more than one go ($500 from recycling).
/// It gives up after a few goes, and when needs nest too deep, which is what a need that
/// needs itself would do.
/// </summary>
public class NeedStep : BotStep
{
    private const int MaxGoes = 5;
    private const int MaxDepth = 4;

    private readonly BotFact _fact;
    private readonly int _goes;

    public NeedStep(BotFact fact)
        : this(fact, 0)
    {
    }

    private NeedStep(BotFact fact, int goes)
        : base("need " + fact.Name, DefaultTimeLimit)
    {
        _fact = fact;
        _goes = goes;
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        if (_fact.Holds(body))
        {
            body.Events.Write("need", _fact.Name + ": holds");
            return BotStepState.Done;
        }

        if (_goes >= MaxGoes)
        {
            return Fail("still not " + _fact.Name + " after " + _goes + " goes");
        }

        if (body.NeedDepth >= MaxDepth)
        {
            return Fail("needs within needs, " + MaxDepth + " deep: does something need itself?");
        }

        List<BotActivity> providers = BotActivities.ProvidersOf(_fact.Key);

        if (providers.Count == 0)
        {
            return Fail("nothing provides " + _fact.Key);
        }

        BotActivity provider = providers[body.Random.Next(providers.Count)];
        body.Events.Write("need", _fact.Name + ": from \"" + provider.Name + "\"");
        List<BotStep> steps = provider.Steps();
        steps.Add(new NeedStep(_fact, _goes + 1));
        body.NeedDepth++;
        body.Run!.InsertNext(steps);
        return BotStepState.Done;
    }

    public override void End(BotBody body)
    {
        // A later go ends the nesting that the first opened.
        if (_goes > 0 && body.NeedDepth > 0)
        {
            body.NeedDepth--;
        }
    }
}
