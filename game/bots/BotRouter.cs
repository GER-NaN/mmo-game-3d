namespace MmoGame3d.Bots;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Rules.World;
using MmoGame3d.Zones;

/// <summary>
/// The zones and the doors between them (bots.md T3), read once from the zone scenes
/// without loading them: each door under a zone's "Doors" node names its TargetZone.
/// Answers which zone to walk into next on the way to another.
/// </summary>
public static class BotRouter
{
    private static Dictionary<string, List<string>>? _doors;

    // The next zone on the shortest way from one zone to another, or null if none leads
    // there.
    public static string? Next(string from, string to)
    {
        Dictionary<string, List<string>> doors = Doors();
        Dictionary<string, string> cameFrom = new Dictionary<string, string>();
        Queue<string> open = new Queue<string>();
        open.Enqueue(from);
        cameFrom[from] = from;

        while (open.Count > 0)
        {
            string zone = open.Dequeue();

            if (zone == to)
            {
                string step = to;

                while (cameFrom[step] != from)
                {
                    step = cameFrom[step];
                }

                return step;
            }

            foreach (string next in DoorsFrom(doors, zone))
            {
                if (!cameFrom.ContainsKey(next))
                {
                    cameFrom[next] = zone;
                    open.Enqueue(next);
                }
            }
        }

        return null;
    }

    // Any zone a door leads to from this one, or null.
    public static string? AnyNeighbour(string zone)
    {
        List<string> next = DoorsFrom(Doors(), zone);
        return next.Count > 0 ? next[0] : null;
    }

    // The door in this loaded zone that leads to the given zone.
    public static Door? DoorTo(Zone zone, string targetZone)
    {
        Node? doors = zone.GetNodeOrNull("Doors");

        if (doors == null)
        {
            return null;
        }

        foreach (Node child in doors.GetChildren())
        {
            Door? door = child as Door;

            if (door != null && door.TargetZone == targetZone)
            {
                return door;
            }
        }

        return null;
    }

    private static List<string> DoorsFrom(Dictionary<string, List<string>> doors, string zone)
    {
        List<string>? next;
        return doors.TryGetValue(ZoneIds.SceneOf(zone), out next) ? next : new List<string>();
    }

    private static Dictionary<string, List<string>> Doors()
    {
        if (_doors != null)
        {
            return _doors;
        }

        _doors = new Dictionary<string, List<string>>();

        foreach (string zone in ZoneIds.All)
        {
            string scene = ZoneIds.SceneOf(zone);
            List<string> targets = new List<string>();
            SceneState state = GD.Load<PackedScene>(World.ScenePath(scene)).GetState();

            for (int node = 0; node < state.GetNodeCount(); node++)
            {
                // The parent path reads "./Doors" for a door placed under the zone's Doors.
                if (!state.GetNodePath(node, true).ToString().EndsWith("Doors"))
                {
                    continue;
                }

                for (int property = 0; property < state.GetNodePropertyCount(node); property++)
                {
                    if (state.GetNodePropertyName(node, property) == "TargetZone")
                    {
                        targets.Add(state.GetNodePropertyValue(node, property).AsString());
                    }
                }
            }

            _doors[scene] = targets;
        }

        return _doors;
    }
}
