namespace MmoGame3d.Bots;

using Godot;
using MmoGame3d.Drones;
using MmoGame3d.Players;
using MmoGame3d.Zones;

/// <summary>
/// Hunts a drone with the EMP (worn already): walks under the nearest drone still flying,
/// and fires once it is close, as a player would, following it as it moves. Done when the
/// server says one dropped. Fails when the zone has no drone flying.
/// </summary>
public class HuntStep : BotStep
{
    private const double LookInterval = 0.5;

    // Within the EMP's reach (10 m on the server), with room for the drone to move.
    private const float FireWithin = 7f;
    private const double FireEvery = 2.5;

    private double _sinceLook = LookInterval;
    private double _sinceFire = FireEvery;
    private Drone? _target;
    private int _noticesAt = -1;

    public HuntStep()
        : base("hunt a drone with the EMP", 90)
    {
    }

    public override BotIntent Intent
    {
        get { return BotIntent.Walking; }
    }

    public override void Start(BotBody body)
    {
        _noticesAt = body.View?.NoticeCount ?? 0;
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        body.Navigator.Tick(body, delta);
        _sinceLook += delta;
        _sinceFire += delta;

        foreach (string notice in body.NoticesSince(_noticesAt))
        {
            if (notice.Contains("drop out of the sky") || notice.Contains("drops out of the sky"))
            {
                body.Events.Write("notice", notice);
                return BotStepState.Done;
            }
        }

        if (_sinceLook < LookInterval)
        {
            return BotStepState.Running;
        }

        _sinceLook = 0;
        Zone? zone = body.Zone;
        Player? player = body.Player;

        if (zone == null || player == null)
        {
            return BotStepState.Running;
        }

        if (_target == null || !GodotObject.IsInstanceValid(_target) || _target.Down)
        {
            _target = Nearest(zone, player.GlobalPosition);

            if (_target == null)
            {
                return Fail("no drone flying in " + zone.ZoneId);
            }
        }

        Vector3 under = new Vector3(_target.GlobalPosition.X, player.GlobalPosition.Y, _target.GlobalPosition.Z);
        body.Navigator.Go(zone, under, true);

        if (_target.GlobalPosition.DistanceTo(player.GlobalPosition) <= FireWithin && _sinceFire >= FireEvery)
        {
            _sinceFire = 0;
            body.Events.Write("firing", "the EMP at " + _target.Name);
            body.Key("emp", true);
            body.Key("emp", false);
        }

        return BotStepState.Running;
    }

    public override void End(BotBody body)
    {
        body.Navigator.Stop(body);
    }

    private static Drone? Nearest(Zone zone, Vector3 from)
    {
        Node? drones = zone.GetNodeOrNull("Drones");
        Drone? best = null;

        if (drones == null)
        {
            return null;
        }

        foreach (Node child in drones.GetChildren())
        {
            Drone? drone = child as Drone;

            if (drone != null && !drone.Down && (best == null || drone.GlobalPosition.DistanceTo(from) < best.GlobalPosition.DistanceTo(from)))
            {
                best = drone;
            }
        }

        return best;
    }
}
