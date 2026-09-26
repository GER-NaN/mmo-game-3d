namespace MmoGame3d.Zones;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Rules.World;

/// <summary>
/// Holds the loaded zones. The server loads every zone, each far from the others so
/// their collision never meets; a client loads only the one its player is in, at the
/// origin. A zone's node is named after its id, so a synced node has the same path on
/// both sides, and synced positions are zone-local, so they mean the same on both.
/// </summary>
public partial class World : Node3D
{
    // How far apart the server lays zones out. Larger than any zone.
    public const float ZoneSpacing = 1000f;

    private readonly Dictionary<string, Zone> _zones = new Dictionary<string, Zone>();

    public Zone LoadZone(string zoneId, Vector3 offset)
    {
        if (_zones.TryGetValue(zoneId, out Zone? loaded))
        {
            return loaded;
        }

        // An instance (taxi-3) is made from its scene (taxi) under its own name.
        PackedScene scene = GD.Load<PackedScene>(ScenePath(ZoneIds.SceneOf(zoneId)));
        Zone zone = scene.Instantiate<Zone>();
        zone.Name = zoneId;
        zone.Position = offset;
        AddChild(zone);
        _zones[zoneId] = zone;
        return zone;
    }

    // Out of the tree at once, so nothing in it runs another frame.
    public void UnloadZone(string zoneId)
    {
        Zone? zone;

        if (_zones.TryGetValue(zoneId, out zone))
        {
            _zones.Remove(zoneId);
            RemoveChild(zone);
            zone.QueueFree();
        }
    }

    public Zone? GetZone(string zoneId)
    {
        _zones.TryGetValue(zoneId, out Zone? zone);
        return zone;
    }

    public static string ScenePath(string scene)
    {
        return "res://game/zones/" + scene + "/" + scene + ".tscn";
    }
}
