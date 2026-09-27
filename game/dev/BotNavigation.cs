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
    private ulong _bakedMsec;
    private Vector3 _probe;

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
            // Whole cells (0.25 m), which the bake rounds to anyway, with a warning and a
            // backtrace each that crowded the findings' logs.
            AgentRadius = 0.5f,
            AgentHeight = 2f,
            AgentMaxClimb = 0.25f,
            AgentMaxSlope = 40f,
            GeometryParsedGeometryType = NavigationMesh.ParsedGeometryType.StaticColliders,
            GeometryCollisionMask = PhysicsLayers.World,
        };
        NavigationMeshSourceGeometryData3D source = new NavigationMeshSourceGeometryData3D();
        NavigationServer3D.ParseSourceGeometryData(mesh, source, zone);
        CarveDoors(zone, source);
        NavigationServer3D.BakeFromSourceGeometryData(mesh, source);

        // The geometry is parsed relative to the zone, so the region stands where it does.
        _map = zone.GetWorld3D().NavigationMap;
        _region = NavigationServer3D.RegionCreate();
        NavigationServer3D.RegionSetMap(_region, _map);
        NavigationServer3D.RegionSetTransform(_region, zone.GlobalTransform);
        // Built now, not in the background: the map takes an unfinished region in as empty.
        NavigationServer3D.RegionSetUseAsyncIterations(_region, false);
        NavigationServer3D.RegionSetNavigationMesh(_region, mesh);
        _bakedMsec = Time.GetTicksMsec();
        // A probe for Pending: the vertex farthest from the origin, since an empty map
        // answers every nearest point with the origin, and a probe near it looks found.
        Vector3[] baked = mesh.GetVertices();
        _probe = Vector3.Zero;

        foreach (Vector3 vertex in baked)
        {
            Vector3 global = zone.GlobalTransform * vertex;

            if (global.LengthSquared() > _probe.LengthSquared())
            {
                _probe = global;
            }
        }
        _ready = mesh.GetPolygonCount() > 0;
        Aabb extent = new Aabb();
        Vector3[] vertices = mesh.GetVertices();

        for (int i = 0; i < vertices.Length; i++)
        {
            extent = i == 0 ? new Aabb(vertices[i], Vector3.Zero) : extent.Expand(vertices[i]);
        }

        GD.Print("Bot: navigation for " + zone.ZoneId + ": " + mesh.GetPolygonCount() + " polygons in " + (Time.GetTicksMsec() - started) + " ms, x "
            + extent.Position.X.ToString("0.0") + " to " + extent.End.X.ToString("0.0") + ", y " + extent.Position.Y.ToString("0.0") + " to " + extent.End.Y.ToString("0.0")
            + ", z " + extent.Position.Z.ToString("0.0") + " to " + extent.End.Z.ToString("0.0"));
    }

    // Doors are cut out of the walkable area, so a path never crosses one it does not mean
    // to use (a door's trigger reaches out onto the pavement and swallows passers-by). A
    // walk to a door still gets there: past the path's end it steers straight in.
    private static void CarveDoors(Zone zone, NavigationMeshSourceGeometryData3D source)
    {
        Node? doors = zone.GetNodeOrNull("Doors");

        if (doors == null)
        {
            return;
        }

        foreach (Node node in doors.GetChildren())
        {
            Area3D? door = node as Area3D;
            CollisionShape3D? shape = door?.GetNodeOrNull<CollisionShape3D>("Shape");
            BoxShape3D? box = shape?.Shape as BoxShape3D;

            if (door == null || shape == null || box == null)
            {
                continue;
            }

            // The box's footprint, in the zone's own space. A carve is not grown by the
            // agent radius, and paths hug its corners: padded by a body's width and a
            // stride more, or a bot passing the door grazes it and goes through.
            Vector3 half = (box.Size / 2f) + new Vector3(1.5f, 0f, 1.5f);
            Transform3D toZone = zone.GlobalTransform.AffineInverse() * shape.GlobalTransform;
            Vector3[] corners =
            {
                toZone * new Vector3(-half.X, 0f, -half.Z),
                toZone * new Vector3(half.X, 0f, -half.Z),
                toZone * new Vector3(half.X, 0f, half.Z),
                toZone * new Vector3(-half.X, 0f, half.Z),
            };
            float bottom = (toZone * new Vector3(0f, -half.Y, 0f)).Y;
            source.AddProjectedObstruction(corners, bottom, box.Size.Y, true);
        }
    }

    // Whether this zone has a mesh (open land and zones too big to bake have none).
    public bool Ready
    {
        get { return _ready; }
    }

    // Baked, but not yet in the map: it takes the region in on a later physics frame
    // (MapForceUpdate or not), and until then every path is empty, and a walk would go
    // straight, through doors too. Known by asking the map for a point of the mesh.
    // PendingAtMost, should it never come.
    private const ulong PendingAtMost = 3000;

    public bool Pending
    {
        get { return _ready && NavigationServer3D.MapGetClosestPoint(_map, _probe).DistanceTo(_probe) > 1f && Time.GetTicksMsec() - _bakedMsec < PendingAtMost; }
    }

    // The nearest point of the mesh, for the log when a path fails.
    public Vector3 Closest(Vector3 to)
    {
        return _ready ? NavigationServer3D.MapGetClosestPoint(_map, to) : to;
    }

    // What the navigation map holds round a spot, as the paths see it: a text map, 0.5 m a
    // cell, # where the map has walkable ground within a cell of the point (at the height
    // of the spot), S and T for the two points given. For the log when a path fails, so a
    // finding shows whether the ground is missing or the map is.
    public string MapAround(Vector3 start, Vector3 target)
    {
        if (!_ready)
        {
            return "(no mesh here)";
        }

        const float Cell = 0.5f;
        Vector3 middle = (start + target) / 2f;
        float half = Mathf.Clamp(start.DistanceTo(target) / 2f + 3f, 4f, 12f);
        System.Text.StringBuilder map = new System.Text.StringBuilder();
        map.Append("x " + (middle.X - half).ToString("0") + " to " + (middle.X + half).ToString("0") + ", z " + (middle.Z - half).ToString("0") + " (top) to " + (middle.Z + half).ToString("0") + "\n");

        for (float z = middle.Z - half; z <= middle.Z + half; z += Cell)
        {
            for (float x = middle.X - half; x <= middle.X + half; x += Cell)
            {
                Vector3 point = new Vector3(x, start.Y, z);
                char mark = NavigationServer3D.MapGetClosestPoint(_map, point).DistanceTo(point) < Cell + 0.5f ? '#' : '.';

                if (new Vector2(x - start.X, z - start.Z).Length() < Cell * 0.75f)
                {
                    mark = 'S';
                }
                else if (new Vector2(x - target.X, z - target.Z).Length() < Cell * 0.75f)
                {
                    mark = 'T';
                }

                map.Append(mark);
            }

            map.Append('\n');
        }

        return map.ToString();
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
