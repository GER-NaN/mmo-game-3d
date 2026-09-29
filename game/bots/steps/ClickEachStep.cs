namespace MmoGame3d.Bots;

using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Clicks each button in a list, one after another with a pause between, as a curious
/// player tries every one: a terminal's apps, say. The buttons anywhere under the list are
/// gathered once, by their path, since a click may build the list anew; the ones the
/// check turns down (Quit, Delete) are left alone.
/// </summary>
public class ClickEachStep : BotStep
{
    private const double Pause = 0.8;

    private readonly Func<BotBody, Node?> _list;
    private readonly Func<Button, bool> _check;
    private readonly List<NodePath> _paths = new List<NodePath>();
    private double _sinceClick = Pause;
    private bool _gathered;
    private int _next;

    public ClickEachStep(string what, Func<BotBody, Node?> list, Func<Button, bool> check)
        : base("click each of " + what, 60)
    {
        _list = list;
        _check = check;
    }

    public override BotIntent Intent
    {
        get { return BotIntent.Screen; }
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        _sinceClick += delta;

        if (_sinceClick < Pause)
        {
            return BotStepState.Running;
        }

        Node? list = _list(body);

        if (list == null)
        {
            return _gathered ? Fail("the screen closed before every button was tried") : BotStepState.Running;
        }

        if (!_gathered)
        {
            _gathered = true;

            Gather(list, list);

            // A pause before the first click too: a screen opened this frame has no layout.
            _sinceClick = 0;
            return BotStepState.Running;
        }

        if (_next >= _paths.Count)
        {
            return BotStepState.Done;
        }

        Button? current = list.GetNodeOrNull<Button>(_paths[_next]);
        _next++;
        _sinceClick = 0;

        if (current != null && current.IsVisibleInTree())
        {
            body.Events.Write("clicked", current.Text);
            body.Click(current);
        }

        return BotStepState.Running;
    }

    private void Gather(Node list, Node under)
    {
        foreach (Node child in under.GetChildren())
        {
            Button? button = child as Button;

            if (button != null && _check(button))
            {
                _paths.Add(list.GetPathTo(button));
            }

            Gather(list, child);
        }
    }
}
