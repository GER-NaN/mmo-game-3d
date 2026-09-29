namespace MmoGame3d.Bots;

using System;
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
/// for the next. A thing that walks (a townsperson) is followed. The client offers the
/// nearest thing in reach (InteractionFinder), so with another thing nearer (the visitor
/// book beside the subway wall) it walks on closer to its own.
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

    // Spots are kept this far out of a door's trigger, which would take the bot elsewhere.
    private const float DoorMargin = 0.8f;

    private readonly List<Vector3> _spots = new List<Vector3>();
    private string _zoneId = "";
    private readonly string _which;
    private readonly Func<T, bool> _check;
    private T? _target;
    private Vector3 _laidAround;
    private int _spot;
    private double _inReachFor;
    private Vector3 _lastPlace;
    private double _still;

    // In reach, but something else is nearer: walking straight at the thing.
    private bool _closingIn;

    // which: words for the check, for the step's name ("broken"); empty for any.
    public ApproachStep(string which, Func<T, bool> check)
        : base("approach the nearest " + (which.Length > 0 ? which + " " : "") + typeof(T).Name, 60)
    {
        _which = which;
        _check = check;
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

        if (_zoneId.Length == 0)
        {
            _zoneId = zone.ZoneId;
        }
        else if (zone.ZoneId != _zoneId)
        {
            return Fail("a door took it from " + _zoneId + " to " + zone.ZoneId + " on the way");
        }

        // The zone's scene can be made anew (a door, back again), and its things with it.
        if (_target != null && (!GodotObject.IsInstanceValid(_target) || !_target.IsInsideTree()))
        {
            _target = null;
        }

        if (_target == null)
        {
            _target = Nearest(zone, player.GlobalPosition);

            if (_target == null)
            {
                return Fail("no " + (_which.Length > 0 ? _which + " " : "") + typeof(T).Name + " in " + zone.ZoneId);
            }

            body.Events.Write("approaching", _target.Name);
            LaySpots(body, zone, player.GlobalPosition);
        }

        Vector3 feet = player.GlobalPosition;

        if (_target.IsInReach(feet) && NearestInReach(zone, feet) == _target)
        {
            body.Navigator.Stop(body);
            _inReachFor += delta;
            return _inReachFor >= OfferSeconds ? BotStepState.Done : BotStepState.Running;
        }

        _inReachFor = 0;

        if (_target.IsInReach(feet) && !_closingIn)
        {
            _closingIn = true;
            body.Navigator.Go(zone, _target.GlobalPosition, true);
            body.Navigator.StraightOn();
        }

        if (Flat(_target.GlobalPosition, _laidAround) > Moved)
        {
            LaySpots(body, zone, feet);
        }

        if (Stuck(feet, delta) || body.Navigator.Arrived)
        {
            _spot++;
            _closingIn = false;

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
            Vector3 spot = _laidAround + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);

            if (!InADoor(zone, spot))
            {
                _spots.Add(spot);
            }
        }

        // Every spot in a door: the thing itself, walked straight at.
        if (_spots.Count == 0)
        {
            _spots.Add(_laidAround);
        }

        _spots.Sort((a, b) => Flat(a, from).CompareTo(Flat(b, from)));
        _spot = 0;
        _still = 0;
        _lastPlace = from;
        body.Navigator.Go(zone, _spots[0], false);
    }

    // Within a door's trigger, or near it.
    private static bool InADoor(Zone zone, Vector3 spot)
    {
        Node? doors = zone.GetNodeOrNull("Doors");

        if (doors == null)
        {
            return false;
        }

        foreach (Node door in doors.GetChildren())
        {
            CollisionShape3D? shape = door.GetNodeOrNull<CollisionShape3D>("Shape");
            BoxShape3D? box = shape?.Shape as BoxShape3D;

            if (shape == null || box == null)
            {
                continue;
            }

            Vector3 local = shape.ToLocal(spot);

            if (Mathf.Abs(local.X) <= (box.Size.X / 2) + DoorMargin && Mathf.Abs(local.Z) <= (box.Size.Z / 2) + DoorMargin)
            {
                return true;
            }
        }

        return false;
    }

    // The thing the client offers from here: the nearest in reach, as InteractionFinder
    // finds it.
    private static Interactable? NearestInReach(Zone zone, Vector3 feet)
    {
        Node? things = zone.GetNodeOrNull(Interactable.ParentName);
        Interactable? best = null;

        if (things == null)
        {
            return null;
        }

        foreach (Node child in things.GetChildren())
        {
            Interactable? thing = child as Interactable;

            if (thing != null && thing.IsInReach(feet) && (best == null || thing.GlobalPosition.DistanceTo(feet) < best.GlobalPosition.DistanceTo(feet)))
            {
                best = thing;
            }
        }

        return best;
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

    private T? Nearest(Zone zone, Vector3 from)
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

            if (thing != null && _check(thing) && (best == null || thing.GlobalPosition.DistanceTo(from) < best.GlobalPosition.DistanceTo(from)))
            {
                best = thing;
            }
        }

        return best;
    }
}
