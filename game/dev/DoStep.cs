namespace MmoGame3d.Dev;

using System;

/// <summary>A step written in place, for the one-off ones.</summary>
public sealed class DoStep : BotStep
{
    private readonly Func<BotBody, double, StepResult> _tick;
    private readonly bool _movesZone;

    public DoStep(string name, double limit, Func<BotBody, double, StepResult> tick, bool movesZone = false)
        : base(name, limit)
    {
        _tick = tick;
        _movesZone = movesZone;
    }

    public override bool MovesZone
    {
        get { return _movesZone; }
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        return _tick(body, delta);
    }
}
