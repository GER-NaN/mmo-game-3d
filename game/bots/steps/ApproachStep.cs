namespace MmoGame3d.Bots;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Interact;
using MmoGame3d.Players;
using MmoGame3d.Zones;

/// <summary>
/// Walks up to the nearest thing of this type in the zone (a terminal, a shopkeeper, a
/// townsperson) until it is in reach, then waits a moment so the client offers it (the
/// prompt), ready for the interact key. It aims at spots around the thing, just inside its
/// reach, nearest first: something may stand in the way from one side (a lamp post in
/// front of the recycler), and a player walks round. A spot it cannot get to, it leaves
/// for the next. A thing that walks (a townsperson) is followed.
/// </summary>
public class ApproachStep<T> : BotStep
    where T : Interactable
{
    private const int Spots = 8;

    // The spots stand this far inside the reach.
    private const float Inside = 0.6f;

    // The client looks for the nearest thing in reach every 0.1 s.
    private const double OfferSeconds = 0.3;

    // Not moving this long gives up the spot.
    private const double StuckSeconds = 3;
    private const float StuckDistance = 0.2f;

    // A walking thing that has gone this far from where the spots were laid is followed.
    private const float Moved = 1.5f;

    private readonly List<Vector3> _spots = new List<Vector3>();
    private T? _target;
    private Vector3 _laidAround;
    private int _spot;
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
            LaySpots(body, zone, player.GlobalPosition);
        }

        Vector3 feet = player.GlobalPosition;

        if (_target.IsInReach(feet))
        {
            body.Navigator.Stop(body);
            _inReachFor += delta;
            return _inReachFor >= OfferSeconds ? BotStepState.Done : BotStepState.Running;
        }

        _inReachFor = 0;

        if (Flat(_target.GlobalPosition, _laidAround) > Moved)
        {
            LaySpots(body, zone, feet);
        }

        if (Stuck(feet, delta) || body.Navigator.Arrived)
        {
            _spot++;

            if (_spot >= _spots.Count)
            {
                return Fail("no spot in reach of " + _target.Name + " could be walked to; last stopped " + Flat(feet, _target.GlobalPosition).ToString("0.0") + " m from it (reach " + _target.Reach + " m)");
            }

            body.Navigator.Go(zone, _spots[_spot], false);
        }

        body.Navigator.Tick(body, delta);
        return BotStepState.Running;
    }

    public override void End(BotBody body)
    {
        body.Navigator.Stop(body);
    }

    // Spots round the thing, just inside its reach, nearest to the player first, and off
    // to the first of them.
    private void LaySpots(BotBody body, Zone zone, Vector3 from)
    {
        _spots.Clear();
        _laidAround = _target!.GlobalPosition;
        float radius = Mathf.Max(0.5f, _target.Reach - Inside);

        for (int i = 0; i < Spots; i++)
        {
            float angle = i * Mathf.Tau / Spots;
            _spots.Add(_laidAround + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius));
        }

        _spots.Sort((a, b) => Flat(a, from).CompareTo(Flat(b, from)));
        _spot = 0;
        _still = 0;
        _lastPlace = from;
        body.Navigator.Go(zone, _spots[0], false);
    }

    private bool Stuck(Vector3 feet, double delta)
    {
        if (feet.DistanceTo(_lastPlace) >= StuckDistance)
        {
            _lastPlace = feet;
            _still = 0;
            return false;
        }

        _still += delta;

        if (_still < StuckSeconds)
        {
            return false;
        }

        _still = 0;
        return true;
    }

    private static float Flat(Vector3 a, Vector3 b)
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
