namespace MmoGame3d.Bots;

using Godot;
using MmoGame3d.Zones;

/// <summary>
/// Walks into a zone through its door, by way of any zones between (BotRouter), walking
/// inside each with the navigator. With walkIn, done on walking in, not on being there: a
/// bot that starts inside the zone goes out to a neighbour and back in. Without, being
/// there is enough.
/// </summary>
public class EnterZoneStep : BotStep
{
    // Time to land in a zone before walking on.
    private const double SettleSeconds = 1;

    // How far before a door the path ends, clear of its trigger.
    private const float FrontDistance = 2.5f;

    private readonly string _target;
    private readonly bool _walkIn;
    private string _zoneId = "";
    private double _settled;
    private bool _walking;
    private Door? _door;
    private bool _inFront;

    public EnterZoneStep(string target, bool walkIn)
        : base((walkIn ? "enter " : "go to ") + target, 120)
    {
        _target = target;
        _walkIn = walkIn;
    }

    public override BotIntent Intent
    {
        get { return BotIntent.Walking; }
    }

    public override void Start(BotBody body)
    {
        _zoneId = body.Zone?.ZoneId ?? "";
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        Zone? zone = body.Zone;

        if (zone == null)
        {
            return BotStepState.Running;
        }

        if (zone.ZoneId != _zoneId)
        {
            body.Navigator.Stop(body);
            _zoneId = zone.ZoneId;
            _walking = false;
            _settled = 0;
            body.Events.Write("arrived", _zoneId);

            if (_zoneId == _target)
            {
                return BotStepState.Done;
            }
        }

        // Already there at the start, without walking in.
        if (!_walkIn && zone.ZoneId == _target)
        {
            return BotStepState.Done;
        }

        if (_walking)
        {
            // At the spot before the door: straight on into it.
            if (!_inFront && body.Navigator.Arrived && _door != null && GodotObject.IsInstanceValid(_door))
            {
                _inFront = true;
                body.Navigator.Go(zone, _door.GlobalPosition, true);
                body.Navigator.StraightOn();
            }

            body.Navigator.Tick(body, delta);
            return BotStepState.Running;
        }

        _settled += delta;

        if (_settled < SettleSeconds)
        {
            return BotStepState.Running;
        }

        string? next = _zoneId == _target ? BotRouter.AnyNeighbour(_zoneId) : BotRouter.Next(_zoneId, _target);
        Door? door = next == null ? null : BotRouter.DoorTo(zone, next);

        if (door == null)
        {
            return Fail("no way from " + _zoneId + " toward " + _target);
        }

        _door = door;
        _inFront = false;
        body.Navigator.Go(zone, InFront(door, body.Player?.GlobalPosition ?? door.GlobalPosition), false);
        body.Events.Write("walking", "to the " + next + " door");
        _walking = true;
        return BotStepState.Running;
    }

    // A spot before the door on the side the player comes from: the path goes there, and
    // then the walk goes straight on into the door, whose trigger is a block to the path.
    private static Vector3 InFront(Door door, Vector3 from)
    {
        Vector3 normal = door.GlobalBasis.Z.Normalized() * FrontDistance;
        Vector3 one = door.GlobalPosition + normal;
        Vector3 other = door.GlobalPosition - normal;
        return one.DistanceTo(from) <= other.DistanceTo(from) ? one : other;
    }

    public override void End(BotBody body)
    {
        body.Navigator.Stop(body);
    }
}
