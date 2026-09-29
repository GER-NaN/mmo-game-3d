namespace MmoGame3d.Bots;

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

    private readonly string _target;
    private readonly bool _walkIn;
    private string _zoneId = "";
    private double _settled;
    private bool _walking;

    public EnterZoneStep(string target, bool walkIn)
        : base((walkIn ? "enter " : "go to ") + target, 120)
    {
        _target = target;
        _walkIn = walkIn;
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

        body.Navigator.Go(zone, door.GlobalPosition, true);
        body.Events.Write("walking", "to the " + next + " door");
        _walking = true;
        return BotStepState.Running;
    }

    public override void End(BotBody body)
    {
        body.Navigator.Stop(body);
    }
}
