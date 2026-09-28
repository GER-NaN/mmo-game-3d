namespace MmoGame3d.Dev;

using System;

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
