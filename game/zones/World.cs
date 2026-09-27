namespace MmoGame3d.Zones;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Rules.World;

/// <summary>
/// Holds the loaded zones. The server loads every zone; a client loads only the one its
/// player is in. Each zone sits at the origin under a holder named after its id, with the
/// zone node itself named "Zone", so a synced node has the same path on both sides
/// (World/town/Zone/Players/1234).
///
/// On the server the holder is a SubViewport with its own World3D, which gives each zone
/// its own physics space: zones are isolated maps and never collide, whatever their size
/// or number. Godot finds a node's world through the nearest viewport, so a viewport is
/// the only way to get a second world (godot-proposals#2638 asks for another). On a client
/// the holder is a plain Node, so the zone renders in the main window.
/// </summary>
public partial class World : Node3D
{
    // Set by the server before it loads anything.
    public bool OwnPhysicsPerZone { get; set; }

    private readonly Dictionary<string, Zone> _zones = new Dictionary<string, Zone>();

    // Every zone scene once loaded, kept for the whole run: a zone a client comes back to
    // is made from the kept scene, not loaded again. Tried against "Handle is not
    // initialized", which a client hit loading Old Town again (bot-testing-findings.md).
    private readonly Dictionary<string, PackedScene> _scenes = new Dictionary<string, PackedScene>();

    public Zone LoadZone(string zoneId)
    {
        if (_zones.TryGetValue(zoneId, out Zone? loaded))
        {
            return loaded;
        }

        // An instance (taxi-3) is made from its scene (taxi) under its own id.
        string sceneId = ZoneIds.SceneOf(zoneId);
        PackedScene? scene;

        if (!_scenes.TryGetValue(sceneId, out scene))
        {
            scene = GD.Load<PackedScene>(ScenePath(sceneId));
            _scenes[sceneId] = scene;
        }
        Zone zone = scene.Instantiate<Zone>();
        zone.Name = "Zone";
        zone.ZoneId = zoneId;

        Node holder = OwnPhysicsPerZone ? NewSpace() : new Node();
        holder.Name = zoneId;
        holder.AddChild(zone);
        AddChild(holder);
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
            Node holder = zone.GetParent();
            RemoveChild(holder);
            holder.QueueFree();
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

    // Nothing is drawn on the server; the viewport is there for its world alone.
    private static SubViewport NewSpace()
    {
        return new SubViewport
        {
            OwnWorld3D = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled,
            Size = new Vector2I(2, 2),
            GuiDisableInput = true,
        };
    }
}
