namespace MmoGame3d.Bots;

using System;
using Godot;

/// <summary>
/// Clicks a control once it can be found: a real click at its centre, so a covered or
/// disabled control does not press, as for a player. The click comes a frame after the
/// control is first seen, since a screen opened this frame has no layout yet and its
/// controls sit at the corner with no size.
/// </summary>
public class ClickStep : BotStep
{
    private const double LookInterval = 0.2;

    private readonly Func<BotBody, Control?> _find;
    private double _sinceLook = LookInterval;
    private Control? _seen;
    private ulong _seenOnFrame;

    public ClickStep(string what, Func<BotBody, Control?> find)
        : base("click " + what, DefaultTimeLimit)
    {
        _find = find;
    }

    public override BotIntent Intent
    {
        get { return BotIntent.Screen; }
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        if (_seen != null && Engine.GetProcessFrames() > _seenOnFrame)
        {
            if (!GodotObject.IsInstanceValid(_seen) || !_seen.IsVisibleInTree())
            {
                _seen = null;
                return BotStepState.Running;
            }

            body.Click(_seen);
            return BotStepState.Done;
        }

        _sinceLook += delta;

        if (_seen != null || _sinceLook < LookInterval)
        {
            return BotStepState.Running;
        }

        _sinceLook = 0;
        Control? control = _find(body);

        if (control != null && control.IsVisibleInTree())
        {
            _seen = control;
            _seenOnFrame = Engine.GetProcessFrames();
        }

        return BotStepState.Running;
    }
}
