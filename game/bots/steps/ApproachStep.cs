namespace MmoGame3d.Bots;

using Godot;
using MmoGame3d.Interact;
using MmoGame3d.Players;
using MmoGame3d.Zones;

/// <summary>
/// Walks up to the nearest thing of this type in the zone (a terminal, a shopkeeper)
/// until it is in reach, then waits a moment so the client offers it (the prompt), ready
/// for the interact key. The path ends at the edge of the walkable ground beside the
/// thing, which can be just outside its reach, so from there it walks straight at the
/// thing until it is in reach or stops moving.
/// </summary>
public class ApproachStep<T> : BotStep
    where T : Interactable
{
    // Inside the reach by this much when it can be, not at its very edge.
    private const float Inside = 0.2f;

    // The client looks for the nearest thing in reach every 0.1 s.
    private const double OfferSeconds = 0.3;

    // Not moving this long, out of reach, fails the step.
    private const double StuckSeconds = 3;
    private const float StuckDistance = 0.2f;

    private T? _target;
    private bool _straight;
    private double _inReachFor;
    private Vector3 _lastPlace;
    private double _still;

    public ApproachStep()
        : base("approach the nearest " + typeof(T).Name, 60)
    {
    }

    public override BotIntent Intent
    {
        get { return BotIntent.Walking; }
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        Zone? zone = body.Zone;
        Player? player = body.Player;

        if (zone == null || player == null)
        {
            return BotStepState.Running;
        }

        if (_target == null)
        {
            _target = Nearest(zone, player.GlobalPosition);

            if (_target == null)
            {
                return Fail("no " + typeof(T).Name + " in " + zone.ZoneId);
            }

            body.Events.Write("approaching", _target.Name);
            body.Navigator.Go(zone, _target.GlobalPosition, false);
            _lastPlace = player.GlobalPosition;
        }

        Vector3 feet = player.GlobalPosition;

        // Well in reach, or in reach and pressed against the thing.
        if (_target.IsInReach(feet, -Inside) || (_target.IsInReach(feet) && _still > OfferSeconds))
        {
            body.Navigator.Stop(body);
            _inReachFor += delta;
            return _inReachFor >= OfferSeconds ? BotStepState.Done : BotStepState.Running;
        }

        if (feet.DistanceTo(_lastPlace) >= StuckDistance)
        {
            _lastPlace = feet;
            _still = 0;
        }
        else
        {
            _still += delta;
        }

        if (_still >= StuckSeconds)
        {
            return Fail("stopped " + FlatDistance(feet, _target.GlobalPosition).ToString("0.0") + " m from " + _target.Name + ", out of its reach of " + _target.Reach + " m");
        }

        if (body.Navigator.Arrived && !_straight)
        {
            _straight = true;
            body.Navigator.Go(zone, _target.GlobalPosition, true);
            body.Navigator.StraightOn();
        }

        body.Navigator.Tick(body, delta);
        return BotStepState.Running;
    }

    public override void End(BotBody body)
    {
        body.Navigator.Stop(body);
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        return new Vector2(a.X - b.X, a.Z - b.Z).Length();
    }

    private static T? Nearest(Zone zone, Vector3 from)
    {
        Node? things = zone.GetNodeOrNull(Interactable.ParentName);
        T? best = null;

        if (things == null)
        {
            return null;
        }

        foreach (Node child in things.GetChildren())
        {
            T? thing = child as T;

            if (thing != null && (best == null || thing.GlobalPosition.DistanceTo(from) < best.GlobalPosition.DistanceTo(from)))
            {
                best = thing;
            }
        }

        return best;
    }
}
