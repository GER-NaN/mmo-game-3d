namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;

/// <summary>
/// An activity as a list of steps, run in turn; each has a time limit, and a step that
/// fails ends it, as does a zone change no step asked for (a party pull). Most activities
/// are one: give Plan the steps, or override Plan.
/// </summary>
public class StepsActivity : BotActivity
{
    private readonly Func<BotBody, bool>? _canStart;
    private readonly Func<BotBody, List<BotStep>>? _plan;
    private readonly string _zone = "";
    private List<BotStep> _steps = new List<BotStep>();
    private int _step;
    private double _inStep;
    private string _zoneNow = "";

    public StepsActivity(string name, int weight, Func<BotBody, bool> canStart, Func<BotBody, List<BotStep>> plan)
        : base(name, weight)
    {
        _canStart = canStart;
        _plan = plan;
    }

    // One that starts in a zone: the router takes the bot there first, from anywhere.
    public StepsActivity(string name, int weight, string zone, Func<BotBody, List<BotStep>> plan)
        : base(name, weight)
    {
        _zone = zone;
        _plan = plan;
    }

    protected StepsActivity(string name, int weight)
        : base(name, weight)
    {
    }

    public override string Zone
    {
        get { return _zone; }
    }

    public override BotStep? Step
    {
        get { return _step < _steps.Count ? _steps[_step] : null; }
    }

    public override bool CanStart(BotBody body)
    {
        return _canStart == null || _canStart(body);
    }

    protected virtual List<BotStep> Plan(BotBody body)
    {
        return _plan != null ? _plan(body) : new List<BotStep>();
    }

    public override void Begin(BotBody body)
    {
        _steps = Plan(body);
        _step = 0;
        _zoneNow = body.ZoneId;
        Why = "";
        FailedWalking = false;
        StartStep(body);
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        if (_step >= _steps.Count)
        {
            return StepResult.Done;
        }

        BotStep step = _steps[_step];

        if (body.ZoneId != _zoneNow && !step.MovesZone)
        {
            Why = "pulled from " + _zoneNow + " to " + body.ZoneId;
            return StepResult.Failed;
        }

        _inStep += delta;
        bool tooLong = _inStep > step.Limit;
        StepResult result = tooLong ? StepResult.Failed : step.Tick(body, delta);

        switch (result)
        {
            case StepResult.Running:
                return StepResult.Running;
            case StepResult.Failed:
                // A walk failed at a thing that is there; a missing one is not a walk.
                FailedWalking = step.Walks && step.Target(body) != null && !tooLong;

                if (FailedWalking)
                {
                    body.WalkFailed?.Invoke(step);
                }

                // A step may have said why already (a refusal it saw).
                if (Why.Length == 0)
                {
                    Why = "\"" + step.Name + "\" " + (tooLong ? "took too long" : "failed");
                }

                return StepResult.Failed;
            default:
                _zoneNow = body.ZoneId;
                _step++;

                if (_step >= _steps.Count)
                {
                    return StepResult.Done;
                }

                StartStep(body);
                return StepResult.Running;
        }
    }

    private void StartStep(BotBody body)
    {
        _inStep = 0;

        if (_step < _steps.Count)
        {
            _steps[_step].Begin(body);
        }
    }
}
