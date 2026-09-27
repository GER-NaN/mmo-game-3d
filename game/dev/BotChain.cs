namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using Godot;

public enum ChainKind
{
    // Written by hand around one piece of state: enroll, change career, enroll back.
    Related,

    // Drawn at random, three to six, with its own seed so it can be run again exactly.
    Random,

    // Planned from facts, and planned again after every activity (BotResolver).
    Goal,
}

/// <summary>
/// Activities one after another, for orders a random pick would almost never reach. Next
/// is asked after each activity for the one after it, with how the last one ended; a
/// failed one does not end the chain, since the next must cope with what it left.
/// </summary>
public abstract class BotChain
{
    protected BotChain(string name, int weight, ChainKind kind)
    {
        Name = name;
        Weight = weight;
        Kind = kind;
    }

    public string Name { get; }

    public int Weight { get; }

    public ChainKind Kind { get; }

    // The bot's own business: out of any party first, and invites turned down.
    public virtual bool Solo
    {
        get { return false; }
    }

    // About how long it takes: a random cancel falls somewhere inside it.
    public virtual double UsualSeconds
    {
        get { return 180; }
    }

    // How it went so far, for the log and findings.
    public List<string> Done { get; } = new List<string>();

    // Why it ended early; "" when it ran out or met its goal.
    public string Why { get; protected set; } = "";

    public virtual bool CanStart(BotBody body)
    {
        return true;
    }

    public virtual void Begin(BotBody body, Random random)
    {
        Done.Clear();
        Why = "";
    }

    // The next activity, or null when the chain is over.
    public BotActivity? Next(BotBody body, BotActivity? last, BotEnd lastEnd)
    {
        if (last != null)
        {
            Done.Add(last.Name + (lastEnd == BotEnd.Finished ? "" : " (" + lastEnd.ToString().ToLowerInvariant() + ")"));
        }

        return NextAfter(body, last, lastEnd);
    }

    protected abstract BotActivity? NextAfter(BotBody body, BotActivity? last, BotEnd lastEnd);

    public string Describe()
    {
        return Name + ": " + (Done.Count == 0 ? "(nothing yet)" : string.Join(" > ", Done));
    }
}

/// <summary>
/// A fixed order, written by hand; each link makes a fresh activity when its turn comes,
/// from the bot as it is then (the career it has, the money).
/// </summary>
public sealed class RelatedChain : BotChain
{
    private readonly Func<BotBody, BotActivity>[] _links;
    private int _next;

    public RelatedChain(string name, int weight, params Func<BotBody, BotActivity>[] links)
        : base(name, weight, ChainKind.Related)
    {
        _links = links;
    }

    public override void Begin(BotBody body, Random random)
    {
        base.Begin(body, random);
        _next = 0;
    }

    protected override BotActivity? NextAfter(BotBody body, BotActivity? last, BotEnd lastEnd)
    {
        return _next < _links.Length ? _links[_next++](body) : null;
    }
}

/// <summary>
/// Three to six activities drawn from those the bot may choose, with a seed of its own: the
/// log line names it, and the same seed draws the same chain again.
/// </summary>
public sealed class RandomChain : BotChain
{
    private readonly Func<IReadOnlyList<BotActivity>> _pool;
    private List<BotActivity> _links = new List<BotActivity>();
    private int _next;

    public RandomChain(string name, int weight, Func<IReadOnlyList<BotActivity>> pool)
        : base(name, weight, ChainKind.Random)
    {
        _pool = pool;
    }

    public int Seed { get; private set; }

    public override void Begin(BotBody body, Random random)
    {
        base.Begin(body, random);
        Seed = random.Next();
        Random draw = new Random(Seed);
        IReadOnlyList<BotActivity> pool = _pool();
        _links = new List<BotActivity>();
        _next = 0;
        int count = 3 + draw.Next(4);

        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            _links.Add(pool[draw.Next(pool.Count)]);
        }

        List<string> names = new List<string>();

        foreach (BotActivity link in _links)
        {
            names.Add(link.Name);
        }

        GD.Print("Bot: random chain, seed " + Seed + ": " + string.Join(", ", names));
    }

    protected override BotActivity? NextAfter(BotBody body, BotActivity? last, BotEnd lastEnd)
    {
        return _next < _links.Count ? _links[_next++] : null;
    }
}

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
    }

    protected override BotActivity? NextAfter(BotBody body, BotActivity? last, BotEnd lastEnd)
    {
        if (last != null)
        {
            _rounds++;
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
}
