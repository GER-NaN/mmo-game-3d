namespace MmoGame3d.Bots;

using System;
using System.Collections.Generic;

/// <summary>
/// One thing bots can do, by name, as a plan of steps (bots.md F5, F7): how to play a
/// feature, with its checks as Until steps. It may provide facts, so a step that needs one
/// can run it first. The plan is built afresh each time, so its steps start new.
/// </summary>
public class BotActivity
{
    private readonly Func<BotPlan, BotPlan> _plan;

    public BotActivity(string name, Func<BotPlan, BotPlan> plan)
        : this(name, new BotFact[0], plan)
    {
    }

    public BotActivity(string name, BotFact[] provides, Func<BotPlan, BotPlan> plan)
    {
        Name = name;
        _plan = plan;
        List<string> keys = new List<string>();

        foreach (BotFact fact in provides)
        {
            keys.Add(fact.Key);
        }

        Provides = keys;
    }

    public string Name { get; }

    // The keys of the facts it makes true ("money", "phone-equipped").
    public IReadOnlyList<string> Provides { get; }

    // What the bot says in chat now and then while it runs this (BotChatter).
    public string[] Phrases { get; private set; } = new string[0];

    public BotActivity Says(params string[] phrases)
    {
        Phrases = phrases;
        return this;
    }

    public List<BotStep> Steps()
    {
        return _plan(new BotPlan()).Steps;
    }
}
