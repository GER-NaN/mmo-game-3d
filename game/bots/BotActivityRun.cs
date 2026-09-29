namespace MmoGame3d.Bots;

using System.Collections.Generic;

/// <summary>
/// One activity being played: its steps, one after another. A step that finishes hands
/// over to the next in the same frame, so nothing the game does at the frame's end
/// (quitting) comes between them. A step past its time limit fails, and so does the run.
/// A need met by another activity inserts that activity's steps next.
/// </summary>
public class BotActivityRun
{
    private readonly List<BotStep> _steps;
    private int _current;
    private bool _started;
    private double _stepTime;

    public BotActivityRun(BotActivity activity)
    {
        Activity = activity;
        _steps = activity.Steps();
    }

    public BotActivity Activity { get; }

    public string FailReason { get; private set; } = "";

    // The step now running, or null between steps.
    public BotStep? Step
    {
        get { return _started ? _steps[_current] : null; }
    }

    // True when the last tick ended a step: a moment the plan can be left or paused.
    public bool AtBoundary { get; private set; }

    public BotStepState Tick(BotBody body, double delta)
    {
        AtBoundary = false;
        double stepDelta = delta;

        while (_current < _steps.Count)
        {
            BotStep step = _steps[_current];

            if (!_started)
            {
                body.Events.Step = step.Name;
                body.Events.Write("step", step.Name);
                _started = true;
                _stepTime = 0;
                step.Start(body);
            }

            _stepTime += stepDelta;
            BotStepState state = step.Tick(body, stepDelta);
            stepDelta = 0;

            if (state == BotStepState.Running && _stepTime > step.TimeLimit)
            {
                state = BotStepState.Failed;
                FailReason = step.Name + ": not done after " + step.TimeLimit + " s";
            }
            else if (state == BotStepState.Failed)
            {
                FailReason = step.Name + ": " + step.FailReason;
            }

            if (state == BotStepState.Running)
            {
                return BotStepState.Running;
            }

            step.End(body);
            _started = false;
            body.Events.Step = "";

            if (state == BotStepState.Failed)
            {
                return BotStepState.Failed;
            }

            _current++;
            AtBoundary = true;
        }

        return BotStepState.Done;
    }

    // Steps to run next, before the rest of the plan.
    public void InsertNext(List<BotStep> steps)
    {
        _steps.InsertRange(_current + 1, steps);
    }

    // Leaves the activity where it is, as a distracted player does.
    public void Cancel(BotBody body)
    {
        if (_started)
        {
            _steps[_current].End(body);
            _started = false;
        }

        body.Events.Step = "";
    }
}
