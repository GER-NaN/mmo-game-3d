namespace MmoGame3d.Bots;

using System;
using Godot;

/// <summary>
/// Clicks a control once it can be found: a real click at its centre, so a covered or
/// disabled control does not press, as for a player. The click comes a frame after the
/// control is first seen, since a screen opened this frame has no layout yet and its
/// controls sit at the corner with no size. Nested containers can take a few frames more
/// to settle, so a control off the window fails only when it stays there. A control in a
/// scroll area is scrolled to with the mouse wheel first, as a player scrolls to it.
/// </summary>
public class ClickStep : BotStep
{
    private const double LookInterval = 0.2;
    private const double SettleTime = 0.5;
    private const int MaxWheels = 40;

    private readonly Func<BotBody, Control?> _find;

    // Where on the control, as fractions of its size; the centre unless given.
    private readonly Vector2 _spot;
    private double _sinceLook = LookInterval;
    private Control? _seen;
    private ulong _seenOnFrame;
    private double _offScreenFor;
    private int _wheels;

    public ClickStep(string what, Func<BotBody, Control?> find)
        : this(what, find, new Vector2(0.5f, 0.5f))
    {
    }

    public ClickStep(string what, Func<BotBody, Control?> find, Vector2 spot)
        : base("click " + what, DefaultTimeLimit)
    {
        _find = find;
        _spot = spot;
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

            // A player cannot click what the window does not show (bots.md R7, off screen).
            Vector2 point = _seen.GetGlobalTransformWithCanvas() * (_seen.Size * _spot);
            Rect2 window = _seen.GetViewport().GetVisibleRect();
            ScrollContainer? scroll = ScrollerOf(_seen);
            Rect2 shown = window;

            if (scroll != null)
            {
                shown = shown.Intersection(new Rect2(scroll.GetGlobalTransformWithCanvas().Origin, scroll.Size));
            }

            if (!shown.HasPoint(point))
            {
                if (scroll != null && _wheels < MaxWheels)
                {
                    _wheels++;
                    body.Wheel(scroll, point.Y > shown.End.Y);
                    return BotStepState.Running;
                }

                _offScreenFor += delta;

                if (_offScreenFor < SettleTime)
                {
                    return BotStepState.Running;
                }

                return Fail(_seen.Name + " is off the screen, at (" + (int)point.X + ", " + (int)point.Y + ") in a window of " + (int)window.Size.X + " by " + (int)window.Size.Y);
            }

            body.ClickAt(_seen, _seen.Size * _spot);
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

    private static ScrollContainer? ScrollerOf(Control control)
    {
        for (Node? node = control.GetParent(); node != null; node = node.GetParent())
        {
            ScrollContainer? scroll = node as ScrollContainer;

            if (scroll != null)
            {
                return scroll;
            }
        }

        return null;
    }
}
