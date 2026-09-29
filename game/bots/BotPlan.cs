namespace MmoGame3d.Bots;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Interact;

/// <summary>
/// Writes a bot's steps as they read:
/// plan.InWorld().Press("inventory").WaitFor&lt;InventoryPanel&gt;().Click(...).
/// Each call adds one step.
/// </summary>
public class BotPlan
{
    public List<BotStep> Steps { get; } = new List<BotStep>();

    public BotPlan Step(BotStep step)
    {
        Steps.Add(step);
        return this;
    }

    public BotPlan InWorld()
    {
        return Step(new InWorldStep());
    }

    public BotPlan WaitFor<T>()
        where T : Control
    {
        return Step(new WaitForScreenStep<T>());
    }

    // A control on a visible screen of this type, by its path there ("%Quit").
    public BotPlan Click<T>(string path)
        where T : Control
    {
        return Step(new ClickStep(path.TrimStart('%') + " on " + typeof(T).Name, body => body.Find<T>()?.GetNodeOrNull<Control>(path)));
    }

    // A control found some other way: a row built from data.
    public BotPlan Click(string what, Func<BotBody, Control?> find)
    {
        return Step(new ClickStep(what, find));
    }

    public BotPlan Press(string action)
    {
        return Step(new KeyStep(action));
    }

    public BotPlan Hold(string action, double seconds)
    {
        return Step(new HoldStep(action, seconds));
    }

    public BotPlan Wait(double seconds)
    {
        return Step(new WaitStep(seconds));
    }

    public BotPlan Until(string what, Func<BotBody, bool> check, double timeLimit = BotStep.DefaultTimeLimit)
    {
        return Step(new UntilStep(what, check, timeLimit));
    }

    public BotPlan Until(BotFact fact, double timeLimit = BotStep.DefaultTimeLimit)
    {
        return Step(new UntilStep(fact.Name, fact.Holds, timeLimit));
    }

    public BotPlan Do(string what, Action<BotBody> action)
    {
        return Step(new DoStep(what, action));
    }

    // Walks in through the zone's door, even when already inside (out and back).
    public BotPlan Enter(string zoneId)
    {
        return Step(new EnterZoneStep(zoneId, true));
    }

    // Walks to the zone if not already in it.
    public BotPlan GoTo(string zoneId)
    {
        return Step(new EnterZoneStep(zoneId, false));
    }

    // Walks until the zone's map is all discovered.
    public BotPlan Survey()
    {
        return Step(new SurveyStep());
    }

    // Walks up to the nearest thing of this type and presses the interact key.
    public BotPlan Use<T>()
        where T : Interactable
    {
        return Step(new ApproachStep<T>()).Press("interact");
    }

    public BotPlan WatchForDrones(int wanted, double timeLimit)
    {
        return Step(new WatchForDronesStep(wanted, timeLimit));
    }

    public BotPlan Need(BotFact fact)
    {
        return Step(new NeedStep(fact));
    }

    public BotPlan ExpectQuit()
    {
        return Step(new ExpectQuitStep());
    }
}
