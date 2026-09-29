namespace MmoGame3d.Bots;

using Godot;
using MmoGame3d.Players;
using MmoGame3d.Zones;

/// <summary>
/// Walks into the corner of a building picked at random, and keeps pushing and jumping
/// there (the wedger, bots.md R3): where players get wedged between buildings or stood on
/// something they should not be. The floating and out-of-bounds watchers record what it
/// finds. Done after a while of pushing, or when a door by the corner takes it away.
/// </summary>
public class WedgeStep : BotStep
{
    private const double PushSeconds = 10;
    private const double JumpEvery = 1.2;
    private const double JumpHold = 0.1;

    private Vector3 _corner;
    private string _zoneId = "";
    private bool _chosen;
    private double _pushed;
    private double _sinceJump;

    public WedgeStep()
        : base("squeeze into a corner", 60)
    {
    }

    public override BotIntent Intent
    {
        get { return BotIntent.Pushing; }
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        Player? player = body.Player;
        Zone? zone = body.Zone;

        if (player == null || zone == null)
        {
            return BotStepState.Running;
        }

        if (_chosen && zone.ZoneId != _zoneId)
        {
            body.Events.Write("squeezed", "through a door into " + zone.ZoneId);
            return BotStepState.Done;
        }

        if (!_chosen)
        {
            Node? buildings = zone.GetNodeOrNull("Buildings");

            if (buildings == null || buildings.GetChildCount() == 0)
            {
                return Fail(zone.ZoneId + " has no buildings to squeeze between");
            }

            Node3D building = (Node3D)buildings.GetChild(body.Random.Next(buildings.GetChildCount()));
            float side = body.Random.Next(2) == 0 ? -1 : 1;

            // A corner of its front: the buildings stand about ten metres wide.
            _corner = building.GlobalPosition + (building.GlobalBasis.X.Normalized() * 5f * side) + (building.GlobalBasis.Z.Normalized() * 5f);
            _chosen = true;
            _zoneId = zone.ZoneId;
            body.Events.Write("squeezing", "at the corner of " + building.Name);
            body.Navigator.Go(zone, _corner, true);
        }

        body.Navigator.Tick(body, delta);

        if (new Vector2(player.GlobalPosition.X - _corner.X, player.GlobalPosition.Z - _corner.Z).Length() > 2f)
        {
            body.Hold("jump", false);
            return BotStepState.Running;
        }

        _pushed += delta;
        _sinceJump += delta;

        if (_sinceJump >= JumpEvery)
        {
            _sinceJump = 0;
        }

        // Held a moment, not tapped in one frame: the game reads the jump in its physics
        // tick.
        body.Hold("jump", _sinceJump < JumpHold);

        return _pushed >= PushSeconds ? BotStepState.Done : BotStepState.Running;
    }

    public override void End(BotBody body)
    {
        body.Navigator.Stop(body);
        body.Hold("jump", false);
    }
}
