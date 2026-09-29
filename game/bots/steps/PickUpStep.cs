namespace MmoGame3d.Bots;

using Godot;
using MmoGame3d.Items;
using MmoGame3d.Zones;

/// <summary>
/// Walks over the nearest thing lying on the ground in this zone, which picks it up (the
/// server notices the touch). Done when that thing is gone from the ground and the bag
/// holds more. If someone else picked it up first, it goes for the next, a few times.
/// Fails when the zone has nothing lying about.
/// </summary>
public class PickUpStep : BotStep
{
    private const double LookInterval = 0.25;

    private const int MaxTries = 3;

    // How long after the thing goes the bag may take to show it.
    private const double BagPatience = 1.5;

    private GroundItem? _target;
    private int _bagBefore;
    private int _tries;
    private double _goneFor;
    private double _sinceLook = LookInterval;

    public PickUpStep()
        : base("pick up the nearest thing", 60)
    {
    }

    public override BotIntent Intent
    {
        get { return BotIntent.Walking; }
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        body.Navigator.Tick(body, delta);
        _sinceLook += delta;

        if (_sinceLook < LookInterval)
        {
            return BotStepState.Running;
        }

        _sinceLook = 0;
        Zone? zone = body.Zone;

        if (zone == null || body.Player == null)
        {
            return BotStepState.Running;
        }

        if (_target != null)
        {
            if (GodotObject.IsInstanceValid(_target) && _target.IsInsideTree())
            {
                return BotStepState.Running;
            }

            body.Navigator.Stop(body);

            // The server tells the bag a moment after the thing goes.
            if (BotFacts.BagCount(body) > _bagBefore)
            {
                return BotStepState.Done;
            }

            _goneFor += LookInterval;

            if (_goneFor < BagPatience)
            {
                return BotStepState.Running;
            }

            _target = null;
            _goneFor = 0;
            _tries++;

            if (_tries >= MaxTries)
            {
                return Fail("someone else picked up each thing first, " + MaxTries + " times");
            }

            body.Events.Write("picking-up", "someone else took it; the next one");
        }

        _target = Nearest(zone, body.Player.GlobalPosition);

        if (_target == null)
        {
            return Fail("nothing lying on the ground in " + zone.ZoneId);
        }

        _bagBefore = BotFacts.BagCount(body);
        body.Events.Write("picking-up", _target.Name + " at " + zone.ToLocal(_target.GlobalPosition).ToString("0.0"));
        body.Navigator.Go(zone, _target.GlobalPosition, true);
        return BotStepState.Running;
    }

    public override void End(BotBody body)
    {
        body.Navigator.Stop(body);
    }

    private static GroundItem? Nearest(Zone zone, Vector3 from)
    {
        Node? items = zone.GetNodeOrNull("Items");
        GroundItem? best = null;

        if (items == null)
        {
            return null;
        }

        foreach (Node child in items.GetChildren())
        {
            GroundItem? item = child as GroundItem;

            if (item != null && (best == null || item.GlobalPosition.DistanceTo(from) < best.GlobalPosition.DistanceTo(from)))
            {
                best = item;
            }
        }

        return best;
    }
}
