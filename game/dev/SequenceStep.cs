namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Steps run in turn as one, or skipped when the bot decides against them as it begins:
/// for a plan that turns on what the bot saw on the way (a world event running).
/// </summary>
public sealed class SequenceStep : BotStep
{
    private readonly Func<BotBody, bool> _when;
    private readonly List<BotStep> _steps;
    private int _step;
    private double _inStep;

    public SequenceStep(string name, double limit, Func<BotBody, bool> when, List<BotStep> steps)
        : base(name, limit)
    {
        _when = when;
        _steps = steps;
    }

    private BotStep? Current
    {
        get { return _step < _steps.Count ? _steps[_step] : null; }
    }

    public override bool MovesZone
    {
        get
        {
            foreach (BotStep step in _steps)
            {
                if (step.MovesZone)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public override bool Walks
    {
        get { return Current != null && Current.Walks; }
    }

    public override bool Presses
    {
        get { return Current != null && Current.Presses; }
    }

    public override Vector3? Target(BotBody body)
    {
        return Current?.Target(body);
    }

    public override void Begin(BotBody body)
    {
        _step = _when(body) ? 0 : _steps.Count;
        _inStep = 0;
        Current?.Begin(body);
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        BotStep? step = Current;

        if (step == null)
        {
            return StepResult.Done;
        }

        _inStep += delta;

        if (_inStep > step.Limit)
        {
            GD.Print("Bot: \"" + step.Name + "\" took too long");
            return StepResult.Failed;
        }

        StepResult result = step.Tick(body, delta);

        if (result != StepResult.Done)
        {
            return result;
        }

        _step++;
        _inStep = 0;
        Current?.Begin(body);
        return Current == null ? StepResult.Done : StepResult.Running;
    }
}
