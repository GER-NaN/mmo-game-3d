namespace MmoGame3d.Dev;

using System.Collections.Generic;
using Godot;
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
