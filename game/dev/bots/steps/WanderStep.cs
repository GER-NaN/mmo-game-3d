namespace MmoGame3d.Dev;
using Godot;
using MmoGame3d.Players;

/// <summary>Wanders for a while: walks, turns, jumps, stands.</summary>
public sealed class WanderStep : BotStep
{
    private readonly double _seconds;
    private double _left;
    private double _spell;
    private Vector3 _spellFrom;
    private bool _spellWalks;

    public WanderStep(double seconds)
        : base("wander", seconds + 5)
    {
        _seconds = seconds;
    }

    public override bool Walks
    {
        get { return true; }
    }

    public override void Begin(BotBody body)
    {
        _left = _seconds;
        _spell = 0;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        _left -= delta;

        if (_left <= 0)
        {
            body.Stop();
            return StepResult.Done;
        }

        _spell -= delta;

        // A door ahead: turn from it, as a person idling about would, rather than
        // leave the zone by chance.
        if (body.DoorAhead(3f))
        {
            body.Stop();
            Input.ActionPress("turn_left");
            _spell = 0.3;
            return StepResult.Running;
        }

        if (_spell > 0)
        {
            return StepResult.Running;
        }

        _spell = 1 + (body.Random.NextDouble() * 3);
        body.Stop();
        Player? me = body.Me;

        // A walk that got nowhere (a parked car, a wall): turn away first, as a person
        // would, not into it again.
        bool blocked = me != null && _spellWalks && me.GlobalPosition.DistanceTo(_spellFrom) < 0.5f;
        _spellFrom = me?.GlobalPosition ?? Vector3.Zero;
        _spellWalks = !blocked;

        if (blocked)
        {
            _spell = 0.5 + (body.Random.NextDouble() * 1.0);
            Input.ActionPress(body.Random.Next(2) == 0 ? "turn_left" : "turn_right");
            return StepResult.Running;
        }

        switch (body.Random.Next(10))
        {
            case 0:
                _spellWalks = false;
                break;
            case 1:
            case 2:
                Input.ActionPress("move_forward");
                Input.ActionPress(body.Random.Next(2) == 0 ? "turn_left" : "turn_right");
                break;
            case 3:
                Input.ActionPress("move_forward");
                Input.ActionPress("jump");
                break;
            default:
                Input.ActionPress("move_forward");
                break;
        }

        return StepResult.Running;
    }
}
