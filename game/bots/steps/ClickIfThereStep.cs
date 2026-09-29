namespace MmoGame3d.Bots;

using System;
using Godot;

/// <summary>
/// Clicks a control if it is there within a moment, and goes on either way: a button that
/// only shows when there is something to do (a job not taken yet).
/// </summary>
public class ClickIfThereStep : BotStep
{
    private const double Patience = 1.5;

    private readonly string _what;
    private readonly Func<BotBody, Control?> _find;
    private double _looked;
    private Control? _seen;
    private ulong _seenOnFrame;

    public ClickIfThereStep(string what, Func<BotBody, Control?> find)
        : base("click " + what + " if it is there", DefaultTimeLimit)
    {
        _what = what;
        _find = find;
    }

    public override BotIntent Intent
    {
        get { return BotIntent.Screen; }
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        // As ClickStep: a frame after it is first seen, so the screen has its layout.
        if (_seen != null && Engine.GetProcessFrames() > _seenOnFrame)
        {
            if (GodotObject.IsInstanceValid(_seen) && _seen.IsVisibleInTree())
            {
                body.Click(_seen);
            }

            return BotStepState.Done;
        }

        _looked += delta;
        Control? control = _find(body);

        if (control != null && control.IsVisibleInTree() && _seen == null)
        {
            _seen = control;
            _seenOnFrame = Engine.GetProcessFrames();
            return BotStepState.Running;
        }

        if (_seen == null && _looked >= Patience)
        {
            body.Events.Write("not-there", _what);
            return BotStepState.Done;
        }

        return BotStepState.Running;
    }
}
