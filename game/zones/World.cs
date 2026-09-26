namespace MmoGame3d.Zones;

using System.Collections.Generic;
using Godot;

/// <summary>
/// Holds the loaded zones. The server loads every zone; a client loads only the one
/// its player is in. A zone's node is named after its id, so a synced node has the same
/// path on both sides.
/// </summary>
public partial class World : Node3D
{
    private readonly Dictionary<string, Zone> _zones = new Dictionary<string, Zone>();

    public Zone LoadZone(string zoneId)
    {
        if (_zones.TryGetValue(zoneId, out Zone? loaded))
        {
            return loaded;
        }

        PackedScene scene = GD.Load<PackedScene>(ScenePath(zoneId));
        Zone zone = scene.Instantiate<Zone>();
        zone.Name = zoneId;
        AddChild(zone);
        _zones[zoneId] = zone;
        return zone;
    }

    public Zone? GetZone(string zoneId)
    {
        _zones.TryGetValue(zoneId, out Zone? zone);
        return zone;
    }

    public static string ScenePath(string zoneId)
    {
        return "res://game/zones/" + zoneId + "/" + zoneId + ".tscn";
    }
}
