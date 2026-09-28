namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using Godot;

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
