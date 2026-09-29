namespace MmoGame3d.Bots;

using Godot;
using MmoGame3d.Players;

/// <summary>
/// Runs straight for the zone's edge in a random direction, jumping now and then, and
/// keeps pushing there (the escaper, bots.md R3): a way out of the world is what it looks
/// for, and the out-of-bounds watcher records one if it finds it. Done after a while.
/// Pushing, not walking: standing pressed on the edge is the point, not being stuck.
/// </summary>
public class EdgeRunStep : BotStep
{
    private const double RunSeconds = 20;
    private const double JumpEvery = 1.5;
    private const double JumpHold = 0.1;

    private float _heading;
    private double _ran;
    private double _sinceJump;
    private bool _chosen;

    public EdgeRunStep()
        : base("run for the edge", RunSeconds + DefaultTimeLimit)
    {
    }

    public override BotIntent Intent
    {
        get { return BotIntent.Pushing; }
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        Player? player = body.Player;

        if (player == null)
        {
            return BotStepState.Running;
        }

        if (!_chosen)
        {
            _chosen = true;
            _heading = (float)(body.Random.NextDouble() * Mathf.Tau) - Mathf.Pi;
            body.Events.Write("running", "for the edge, heading " + Mathf.RadToDeg(_heading).ToString("0") + " degrees");
        }

        float off = Mathf.Wrap(_heading - player.Heading, -Mathf.Pi, Mathf.Pi);
        body.Hold("turn_left", off > 0.08f);
        body.Hold("turn_right", off < -0.08f);
        body.Hold("move_forward", Mathf.Abs(off) < 0.7f);

        _sinceJump += delta;

        if (_sinceJump >= JumpEvery)
        {
            _sinceJump = 0;
        }

        // Held a moment, not tapped in one frame: the game reads the jump in its physics
        // tick.
        body.Hold("jump", _sinceJump < JumpHold);

        _ran += delta;
        return _ran >= RunSeconds ? BotStepState.Done : BotStepState.Running;
    }

    public override void End(BotBody body)
    {
        body.Hold("turn_left", false);
        body.Hold("turn_right", false);
        body.Hold("move_forward", false);
        body.Hold("jump", false);
    }
}
