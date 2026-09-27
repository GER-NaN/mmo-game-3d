namespace MmoGame3d.Dev;

using System;
using Godot;
using MmoGame3d.Zones;

/// <summary>
/// Paths round buildings for a bot, from Godot's navigation server: when the bot's zone
/// changes it bakes a navigation mesh from the zone's static collision (the ground,
/// the buildings, the props: what a player collides with), and a walk asks it for a
/// path. Only the bot's own client does this; the server and players never see it.
///
/// Open land is left out: the meadows are kilometres of hills with nothing to go round,
/// and a mesh that size takes long to bake. There the bot walks straight.
/// </summary>
public sealed class BotNavigation
{
    // Bigger than this, a zone is open land.
    private const float MaxSide = 400f;

    private Zone? _zone;
    private Rid _region;
    private Rid _map;
    private bool _ready;

    public void Tick(BotBody body)
    {
        Zone? zone = body.Zone;

        if (zone == _zone)
        {
            return;
        }

        Clear();
        _zone = zone;

        if (zone == null || !zone.IsInsideTree() || zone.MapSize.X > MaxSide || zone.MapSize.Y > MaxSide)
        {
            return;
        }

        ulong started = Time.GetTicksMsec();
        NavigationMesh mesh = new NavigationMesh
        {
            AgentRadius = 0.45f,
            AgentHeight = 1.8f,
            AgentMaxClimb = 0.45f,
            AgentMaxSlope = 40f,
            GeometryParsedGeometryType = NavigationMesh.ParsedGeometryType.StaticColliders,
            GeometryCollisionMask = PhysicsLayers.World,
        };
        NavigationMeshSourceGeometryData3D source = new NavigationMeshSourceGeometryData3D();
        NavigationServer3D.ParseSourceGeometryData(mesh, source, zone);
        NavigationServer3D.BakeFromSourceGeometryData(mesh, source);

        // The geometry is parsed relative to the zone, so the region stands where it does.
        _map = zone.GetWorld3D().NavigationMap;
        _region = NavigationServer3D.RegionCreate();
        NavigationServer3D.RegionSetMap(_region, _map);
        NavigationServer3D.RegionSetTransform(_region, zone.GlobalTransform);
        NavigationServer3D.RegionSetNavigationMesh(_region, mesh);
        NavigationServer3D.MapForceUpdate(_map);
        _ready = mesh.GetPolygonCount() > 0;
        GD.Print("Bot: navigation for " + zone.ZoneId + ": " + mesh.GetPolygonCount() + " polygons in " + (Time.GetTicksMsec() - started) + " ms");
    }

    // The points to walk through, first to last; empty with no mesh here.
    public Vector3[] Path(Vector3 from, Vector3 to)
    {
        if (!_ready)
        {
            return Array.Empty<Vector3>();
        }

        return NavigationServer3D.MapGetPath(_map, from, to, true);
    }

    public void Clear()
    {
        if (_region.IsValid)
        {
            NavigationServer3D.FreeRid(_region);
        }

        _region = new Rid();
        _ready = false;
        _zone = null;
    }
}
