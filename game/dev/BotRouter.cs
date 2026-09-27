namespace MmoGame3d.Dev;

using System.Collections.Generic;
using Godot;
using MmoGame3d.BotJudging;
using MmoGame3d.Players;
using MmoGame3d.Rules.World;
using MmoGame3d.Zones;

/// <summary>
/// Where the zones are and how to get between them: the doors of every zone, read from the
/// zone scenes once (their Doors nodes and each one's TargetZone), without loading a zone.
/// A new door or zone needs no bot code.
/// </summary>
public static class BotRouter
{
    private static Dictionary<string, List<DoorLink>>? _doors;

    // The doors out of each zone.
    public static Dictionary<string, List<DoorLink>> Doors
    {
        get
        {
            if (_doors == null)
            {
                _doors = new Dictionary<string, List<DoorLink>>();

                foreach (string zone in ZoneIds.All)
                {
                    _doors[zone] = ReadDoors(zone);
                }
            }

            return _doors;
        }
    }

    // The doors to go through, in order, from one zone to another; empty when already
    // there, null with no way.
    public static List<string>? Route(string from, string to)
    {
        List<string> route = new List<string>();

        if (from == to)
        {
            return route;
        }

        Dictionary<string, DoorLink?> cameBy = new Dictionary<string, DoorLink?> { { from, null } };
        Dictionary<string, string> cameFrom = new Dictionary<string, string>();
        Queue<string> open = new Queue<string>();
        open.Enqueue(from);

        while (open.Count > 0)
        {
            string zone = open.Dequeue();

            if (zone == to)
            {
                break;
            }

            List<DoorLink>? links;

            if (!Doors.TryGetValue(zone, out links))
            {
                continue;
            }

            foreach (DoorLink link in links)
            {
                if (!cameBy.ContainsKey(link.Target))
                {
                    cameBy[link.Target] = link;
                    cameFrom[link.Target] = zone;
                    open.Enqueue(link.Target);
                }
            }
        }

        if (!cameBy.ContainsKey(to))
        {
            return null;
        }

        string at = to;

        while (at != from)
        {
            route.Insert(0, cameBy[at]!.Door);
            at = cameFrom[at];
        }

        return route;
    }

    private static List<DoorLink> ReadDoors(string zone)
    {
        List<DoorLink> links = new List<DoorLink>();
        PackedScene? scene = GD.Load<PackedScene>(World.ScenePath(zone));
        SceneState? state = scene?.GetState();

        if (state == null)
        {
            return links;
        }

        for (int node = 0; node < state.GetNodeCount(); node++)
        {
            string parent = state.GetNodePath(node, true).ToString().TrimStart('.', '/');

            if (parent != "Doors")
            {
                continue;
            }

            for (int property = 0; property < state.GetNodePropertyCount(node); property++)
            {
                if (state.GetNodePropertyName(node, property).ToString() == "TargetZone")
                {
                    links.Add(new DoorLink(state.GetNodeName(node).ToString(), state.GetNodePropertyValue(node, property).AsString()));
                }
            }
        }

        return links;
    }
}

/// <summary>A door out of a zone, and the zone it leads to.</summary>
public sealed class DoorLink
{
    public DoorLink(string door, string target)
    {
        Door = door;
        Target = target;
    }

    public string Door { get; }

    public string Target { get; }
}

/// <summary>
/// Getting to a zone: the route from where the bot is, walked door by door (DoorStep). A
/// party pull, a wrong door or a relog lands it somewhere else, and it plans again from
/// there; a taxi ride ends where it ends.
/// </summary>
public sealed class TravelActivity : BotActivity
{
    private const int MaxPlans = 6;

    private readonly string _to;
    private readonly List<BotFact> _gives;
    private DoorStep? _door;
    private string _doorZone = "";
    private double _inDoor;
    private int _plans;

    public TravelActivity(string zoneId)
        : base("travel to " + zoneId, 0)
    {
        _to = zoneId;
        _gives = new List<BotFact> { BotFact.InZone(zoneId) };
    }

    public override IReadOnlyList<BotFact> Gives
    {
        get { return _gives; }
    }

    public override double UsualSeconds
    {
        get { return 60; }
    }

    public override BotStep? Step
    {
        get { return _door; }
    }

    public override void Begin(BotBody body)
    {
        _door = null;
        _plans = 0;
        Why = "";
        FailedWalking = false;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        if (body.ZoneId == _to)
        {
            body.Stop();
            return StepResult.Done;
        }

        if (ZoneIds.IsInstance(body.ZoneId) || body.ZoneId.Length == 0)
        {
            return StepResult.Running;
        }

        if (_door == null || _doorZone != body.ZoneId)
        {
            _plans++;

            if (_plans > MaxPlans)
            {
                Why = "planned " + MaxPlans + " times and still in " + body.ZoneId;
                return StepResult.Failed;
            }

            List<string>? route = BotRouter.Route(body.ZoneId, _to);

            if (route == null || route.Count == 0)
            {
                Why = "no way from " + body.ZoneId + " to " + _to;
                return StepResult.Failed;
            }

            GD.Print("Bot: route to " + _to + ": " + string.Join(", ", route));
            _door = new DoorStep(route[0]);
            _doorZone = body.ZoneId;
            _inDoor = 0;
            _door.Begin(body);
        }

        _inDoor += delta;
        StepResult result = _inDoor > _door.Limit ? StepResult.Failed : _door.Tick(body, delta);

        if (result == StepResult.Failed && _door.Target(body) != null && _inDoor <= _door.Limit)
        {
            FailedWalking = true;
            body.WalkFailed?.Invoke(_door);
        }

        // Done or failed, the next tick plans from wherever the bot is now.
        if (result != StepResult.Running)
        {
            _door = null;
        }

        return StepResult.Running;
    }

    public override BotActivityJudge? NewJudge()
    {
        return new TravelJudge(_to);
    }
}

/// <summary>
/// A traveller as its player sees it: not moving, and not there yet, for a good while, is
/// stuck (TravelWatch decides; this looks).
/// </summary>
public sealed class TravelJudge : BotActivityJudge
{
    private readonly TravelWatch _watch;

    public TravelJudge(string to)
    {
        _watch = new TravelWatch(to);
    }

    public override string? Watch(BotBody body, double delta)
    {
        Player? me = body.Me;

        if (me == null)
        {
            return null;
        }

        Vector3 at = me.GlobalPosition;
        return _watch.Look(delta, body.ZoneId, new System.Numerics.Vector3(at.X, at.Y, at.Z));
    }
}
