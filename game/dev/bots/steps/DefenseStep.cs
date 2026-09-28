namespace MmoGame3d.Dev;

using System;
using Godot;
using MmoGame3d.Rules.Terminals;
using MmoGame3d.Ui;

/// <summary>
/// Plays an Agent Defense run: each cue's lane key as the cue crosses the line, until the
/// run is over.
/// </summary>
public sealed class DefenseStep : BotStep
{
    private int _pressed = -1;
    private bool _started;

    public DefenseStep()
        : base("play the run", 90)
    {
    }

    public override void Begin(BotBody body)
    {
        _pressed = -1;
        _started = false;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        AgentDefenseView? view = body.Me?.GetTree().GetFirstNodeInGroup(AgentDefenseView.Group) as AgentDefenseView;

        if (view == null || !view.Playing)
        {
            // Over once it had started: the server scored it.
            return _started ? StepResult.Done : StepResult.Running;
        }

        _started = true;
        int clock = view.Clock;

        foreach (DefenseCue cue in view.Cues)
        {
            if (cue.AtMs > _pressed && cue.AtMs <= clock)
            {
                string lane = AgentDefenseView.LaneActions[cue.Lane];
                Input.ParseInputEvent(new InputEventAction { Action = lane, Pressed = true });
                Input.ParseInputEvent(new InputEventAction { Action = lane, Pressed = false });
            }
        }

        _pressed = Math.Max(_pressed, clock);
        return StepResult.Running;
    }
}
