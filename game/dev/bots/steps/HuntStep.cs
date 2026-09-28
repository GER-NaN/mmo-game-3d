namespace MmoGame3d.Dev;

using Godot;

/// <summary>Walks under the nearest drone and fires the EMP until it is down.</summary>
public sealed class HuntStep : BotStep
{
    private const float Under = 5f;
    private const double FireEvery = 1.5;

    private Walker _walker = new Walker();
    private Node3D? _drone;
    private double _fireIn;

    public HuntStep()
        : base("hunt a drone", 60)
    {
    }

    public override bool Walks
    {
        get { return true; }
    }

    public override Vector3? Target(BotBody body)
    {
        return _drone != null && GodotObject.IsInstanceValid(_drone) && _drone.IsInsideTree() ? _drone.GlobalPosition : null;
    }

    public override void Begin(BotBody body)
    {
        _drone = body.Nearest(body.LiveDrones());
        _walker = new Walker();
        _fireIn = 0;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        if (_drone == null)
        {
            return StepResult.Failed;
        }

        if (!GodotObject.IsInstanceValid(_drone) || !_drone.IsInsideTree() || ((Drones.Drone)_drone).Down)
        {
            body.Stop();
            GD.Print("Bot: the drone is down");
            return StepResult.Done;
        }

        // The ground under it, at the bot's own height: a drone over a building puts the
        // nearest point of the air on the roof, and the walk presses into the wall.
        Players.Player? me = body.Me;
        Vector3 under = me == null ? _drone.GlobalPosition : new Vector3(_drone.GlobalPosition.X, me.GlobalPosition.Y, _drone.GlobalPosition.Z);
        _walker.Walk(body, under, Under, delta);
        _fireIn -= delta;

        if (_fireIn <= 0 && body.DistanceTo(_drone.GlobalPosition) < Under * 2f)
        {
            _fireIn = FireEvery;
            BotBody.Press("emp");
        }

        return StepResult.Running;
    }
}
