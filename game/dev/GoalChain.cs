namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// A goal: the facts it wants, worked out afresh after every activity (BotResolver), then
/// the activities it wants them for, if any (an EMP worn, then two drones hunted). The facts
/// are set when it starts (money: ten more than now), and a budget caps its activities.
/// </summary>
public sealed class GoalChain : BotChain
{
    private readonly Func<BotBody, bool> _canStart;
    private readonly Func<BotBody, List<BotFact>> _wanted;
    private readonly int _budget;
    private readonly double _usual;
    private readonly Func<BotBody, BotActivity>[] _then;
    private int _thenNext;
    private List<BotFact> _facts = new List<BotFact>();
    private Random _random = new Random();
    private int _rounds;

    // Activities that finished without giving what they promise, by name: twice, and
    // the goal stops asking for them.
    private readonly Dictionary<string, int> _unkept = new Dictionary<string, int>();

    public GoalChain(string name, int weight, Func<BotBody, bool> canStart, Func<BotBody, List<BotFact>> wanted, int budget, double usualSeconds, params Func<BotBody, BotActivity>[] then)
        : base(name, weight, ChainKind.Goal)
    {
        _canStart = canStart;
        _wanted = wanted;
        _budget = budget;
        _usual = usualSeconds;
        _then = then;
    }

    public override bool Solo
    {
        get { return true; }
    }

    public override double UsualSeconds
    {
        get { return _usual; }
    }

    public override bool CanStart(BotBody body)
    {
        return _canStart(body);
    }

    public override void Begin(BotBody body, Random random)
    {
        base.Begin(body, random);
        _facts = _wanted(body);
        _random = random;
        _rounds = 0;
        _thenNext = 0;
        _unkept.Clear();
    }

    protected override BotActivity? NextAfter(BotBody body, BotActivity? last, BotEnd lastEnd)
    {
        if (last != null)
        {
            _rounds++;

            if (lastEnd == BotEnd.Finished && !Kept(body, last))
            {
                int times;
                _unkept.TryGetValue(last.Name, out times);
                _unkept[last.Name] = times + 1;

                if (times + 1 >= 2)
                {
                    Why = "\"" + last.Name + "\" finished twice without giving what it promises";
                    return null;
                }
            }
        }

        if (_rounds >= _budget)
        {
            Why = "out of activities after " + _budget;
            return null;
        }

        string why;
        string plan;
        BotActivity? next = BotResolver.Next(body, _facts, BotCatalog.All.Activities, _random, out why, out plan);
        Why = why;

        if (next != null)
        {
            GD.Print("Bot: plan for \"" + Name + "\": " + plan);
            return next;
        }

        if (why.Length > 0 || _thenNext >= _then.Length)
        {
            return null;
        }

        // The facts hold: now what they were for.
        BotActivity then = _then[_thenNext++](body);

        if (!then.CanStart(body))
        {
            Why = "cannot " + then.Name + " now";
            return null;
        }

        return then;
    }

    private static bool Kept(BotBody body, BotActivity activity)
    {
        foreach (BotFact promise in activity.Gives)
        {
            if (promise.Kind != FactKind.MoneyAtLeast && !promise.IsTrue(body))
            {
                return false;
            }
        }

        return true;
    }
}
