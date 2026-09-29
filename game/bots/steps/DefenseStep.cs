namespace MmoGame3d.Bots;

using MmoGame3d.Ui;

/// <summary>
/// Plays a run of Agent Defense (the run already started): presses the lane keys now and
/// then, at random, as a player new to it would, until the run ends. Done when the view
/// has played and stopped.
/// </summary>
public class DefenseStep : BotStep
{
    private const double PressEvery = 0.25;

    private double _sincePress;
    private bool _sawPlaying;
    private string _held = "";

    public DefenseStep()
        : base("play a run of Agent Defense", 120)
    {
    }

    public override BotIntent Intent
    {
        get { return BotIntent.Screen; }
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        AgentDefenseView? view = body.Find<AgentDefenseView>();

        if (view == null)
        {
            return BotStepState.Running;
        }

        if (!view.Playing)
        {
            return _sawPlaying ? BotStepState.Done : BotStepState.Running;
        }

        _sawPlaying = true;
        _sincePress += delta;

        if (_sincePress < PressEvery)
        {
            return BotStepState.Running;
        }

        _sincePress = 0;

        // Each press is let go on the next, as a tap.
        if (_held.Length > 0)
        {
            body.Key(_held, false);
            _held = "";
            return BotStepState.Running;
        }

        _held = AgentDefenseView.LaneActions[body.Random.Next(AgentDefenseView.LaneActions.Length)];
        body.Key(_held, true);
        return BotStepState.Running;
    }

    public override void End(BotBody body)
    {
        if (_held.Length > 0)
        {
            body.Key(_held, false);
        }
    }
}
