namespace MmoGame3d.Dev;
using Godot;

/// <summary>
/// Runs straight for a point well past the zone's edge, jumping, to find a way out of
/// the world. The position judge reports it if one works (out-of-bounds, no-footing).
/// </summary>
public sealed class EdgeStep : BotStep
{
    private readonly double _seconds;
    private double _left;
    private Vector3 _toward;
    private double _jumpIn;

    public EdgeStep(double seconds)
        : base("run for the edge", seconds + 5)
    {
        _seconds = seconds;
    }

    public override bool Walks
    {
        get { return true; }
    }

    public override bool Presses
    {
        get { return true; }
    }

    public override Vector3? Target(BotBody body)
    {
        return _toward;
    }

    public override void Begin(BotBody body)
    {
        _left = _seconds;
        Zones.Zone? zone = body.Zone;
        float reach = zone == null || zone.MapSize == Vector2.Zero ? 40f : (Mathf.Max(zone.MapSize.X, zone.MapSize.Y) / 2f) + 40f;
        float angle = (float)(body.Random.NextDouble() * Mathf.Tau);
        Vector3 middle = zone == null ? Vector3.Zero : zone.GlobalPosition;
        _toward = middle + (new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * reach);
        GD.Print("Bot: running for the edge toward (" + _toward.X.ToString("0") + ", " + _toward.Z.ToString("0") + ")");
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        _left -= delta;

        if (_left <= 0)
        {
            body.Stop();
            return StepResult.Done;
        }

        // Straight at it, never round: the point is to press against what stops a player.
        body.SteerTo(_toward);
        _jumpIn -= delta;

        if (_jumpIn <= 0)
        {
            _jumpIn = 1 + body.Random.NextDouble() * 2;
            Input.ActionPress("jump");
        }
        else
        {
            Input.ActionRelease("jump");
        }

        return StepResult.Running;
    }
}
