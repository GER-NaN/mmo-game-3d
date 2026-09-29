namespace MmoGame3d.Bots;

using Godot;
using MmoGame3d.Players;
using MmoGame3d.Zones;

/// <summary>
/// Walking inside one zone (bots.md T3a). On the first Go in a zone it bakes a
/// navigation mesh from the zone's static collision onto its own map, and once the map
/// has synced it asks for a path; later Gos in the same zone reuse the mesh. Each frame
/// it steers along the path with the turn and forward keys, as a player would. At the
/// path's end it stops, or with pastEnd keeps walking at the target (into a door's
/// trigger). Where no mesh bakes (terrain has no collision nodes) it walks straight.
/// </summary>
public class BotNavigator
{
    // Past this width the engine refuses to bake at the default cell size, with an error.
    private const float MaxBakeWidth = 300f;

    // A map that has synced can still answer with no path for a moment (seen with many
    // clients starting at once), so it is asked again for this long before walking
    // straight.
    private const double PathRetrySeconds = 3;

    // Close enough to a path point to aim at the next one.
    private const float PointReach = 0.75f;

    // Facing within this of the aim, it walks; past it, it only turns.
    private const float WalkWithin = 0.7f;
    private const float TurnDeadZone = 0.08f;

    // Walking and not getting anywhere for this long, it backs off and sidesteps for a
    // moment, then finds a path again from where it stands (bots.md R8: stuck recovery).
    private const double StuckSeconds = 1.5;
    private const float Progress = 0.3f;
    private const double BackOffSeconds = 0.6;

    // Added to a door's trigger in the bake: across it, and before and behind it.
    private static readonly Vector3 DoorClearance = new Vector3(1f, 0f, 3f);

    private Rid _map;
    private Rid _region;

    // The map's sync count when the mesh was set; a path is asked for once it has moved.
    private long _meshIteration = -1;
    private bool _pathPending;
    private double _pendingFor;
    private Vector3 _target;
    private Vector3[] _path = new Vector3[0];
    private int _pathIndex;
    private bool _going;
    private bool _pastEnd;

    // Walking straight at the target (StraightOn), with no path to find again.
    private bool _straight;

    private Vector3 _lastPlace;
    private double _stillFor;
    private double _backingFor;
    private string _sidestep = "strafe_left";

    // The zone the map holds a mesh of, and whether one baked there at all.
    private Zone? _bakedZone;
    private bool _hasMesh;

    // "straight" or the path's point count, for the events file.
    public string Route { get; private set; } = "";

    // At the end of its path, or of its straight walk. With pastEnd it never is: it
    // walks on until stopped.
    public bool Arrived
    {
        get { return _going && !_pastEnd && !_pathPending && _pathIndex >= _path.Length; }
    }

    public void Go(Zone zone, Vector3 target, bool pastEnd)
    {
        _target = target;
        _pastEnd = pastEnd;
        _path = new Vector3[0];
        _pathIndex = 0;
        _going = true;
        _pendingFor = 0;
        _straight = false;
        _stillFor = 0;
        _backingFor = 0;

        if (_bakedZone != zone || !GodotObject.IsInstanceValid(_bakedZone))
        {
            _bakedZone = zone;
            _hasMesh = Bake(zone);
        }

        _pathPending = _hasMesh;

        if (!_pathPending)
        {
            _path = new Vector3[] { target };
            Route = "straight (no mesh bakes here)";
        }
    }

    // Drops the path and walks straight at the target: the last metres to something the
    // walkable ground stops short of.
    public void StraightOn()
    {
        _straight = true;
        _pathPending = false;
        _path = new Vector3[0];
        _pathIndex = 0;
    }

    public void Tick(BotBody body, double delta)
    {
        Player? player = body.Player;

        if (!_going || player == null)
        {
            return;
        }

        if (_pathPending)
        {
            _pendingFor += delta;

            if (NavigationServer3D.MapGetIterationId(_map) <= _meshIteration)
            {
                return;
            }

            _path = NavigationServer3D.MapGetPath(_map, player.GlobalPosition, _target, true);

            if (_path.Length == 0 && _pendingFor < PathRetrySeconds)
            {
                return;
            }

            _pathPending = false;

            if (_path.Length == 0)
            {
                _path = new Vector3[] { _target };
                Route = "straight (no path found)";
            }
            else
            {
                Route = _path.Length + " path points";
            }

            body.Events.Write("route", Route);
        }

        if (_backingFor > 0)
        {
            BackOff(body, player, delta);
            return;
        }

        Steer(body, player);
        WatchProgress(body, player, delta);
    }

    // Holding forward and not getting anywhere: time to back off.
    private void WatchProgress(BotBody body, Player player, double delta)
    {
        Vector3 here = player.GlobalPosition;

        if (!Input.IsActionPressed("move_forward") || Flat(here, _lastPlace) >= Progress)
        {
            _lastPlace = here;
            _stillFor = 0;
            return;
        }

        _stillFor += delta;

        if (_stillFor < StuckSeconds)
        {
            return;
        }

        _stillFor = 0;
        _backingFor = BackOffSeconds;
        _sidestep = body.Random.Next(2) == 0 ? "strafe_left" : "strafe_right";
        body.Events.Write("unsticking", "backing off to the " + (_sidestep == "strafe_left" ? "left" : "right"));
    }

    // Back and to one side for a moment, then a path again from here.
    private void BackOff(BotBody body, Player player, double delta)
    {
        body.Hold("move_forward", false);
        body.Hold("turn_left", false);
        body.Hold("turn_right", false);
        body.Hold("move_back", true);
        body.Hold(_sidestep, true);
        _backingFor -= delta;

        if (_backingFor > 0)
        {
            return;
        }

        body.Hold("move_back", false);
        body.Hold(_sidestep, false);
        _lastPlace = player.GlobalPosition;

        if (_hasMesh && !_straight)
        {
            Vector3[] path = NavigationServer3D.MapGetPath(_map, player.GlobalPosition, _target, true);

            if (path.Length > 0)
            {
                _path = path;
                _pathIndex = 0;
            }
        }
    }

    public void Stop(BotBody body)
    {
        _going = false;
        _backingFor = 0;
        LetGo(body);
    }

    private static void LetGo(BotBody body)
    {
        body.Hold("turn_left", false);
        body.Hold("turn_right", false);
        body.Hold("move_forward", false);
        body.Hold("move_back", false);
        body.Hold("strafe_left", false);
        body.Hold("strafe_right", false);
    }

    private void Steer(BotBody body, Player player)
    {
        Vector3 here = player.GlobalPosition;

        while (_pathIndex < _path.Length && Flat(here, _path[_pathIndex]) < PointReach)
        {
            _pathIndex++;
        }

        if (_pathIndex >= _path.Length && !_pastEnd)
        {
            LetGo(body);
            return;
        }

        Vector3 aim = _pathIndex < _path.Length ? _path[_pathIndex] : _target;
        Vector3 to = aim - here;
        float wanted = Mathf.Atan2(-to.X, -to.Z);
        float off = Mathf.Wrap(wanted - player.Heading, -Mathf.Pi, Mathf.Pi);

        body.Hold("turn_left", off > TurnDeadZone);
        body.Hold("turn_right", off < -TurnDeadZone);
        body.Hold("move_forward", Mathf.Abs(off) < WalkWithin);
    }

    // False where nothing bakes: terrain has no collision nodes to read, which leaves
    // only the zone's edge walls, too far apart to bake.
    private bool Bake(Zone zone)
    {
        NavigationMesh mesh = new NavigationMesh();

        // Heights are whole cells (0.25 m), or the engine warns that it rounds them. The
        // radius is a cell wider than the body's 0.5 m: the steering turns before it walks
        // and cuts corners, and a path that grazes a tree trunk wedges it there.
        mesh.AgentRadius = 0.75f;
        mesh.AgentHeight = 2f;
        mesh.AgentMaxClimb = 0.5f;
        mesh.GeometryParsedGeometryType = NavigationMesh.ParsedGeometryType.StaticColliders;

        NavigationMeshSourceGeometryData3D source = new NavigationMeshSourceGeometryData3D();
        NavigationServer3D.ParseSourceGeometryData(mesh, source, zone);
        AddDoors(zone, source);
        Vector3 size = source.GetBounds().Size;

        if (size.X > MaxBakeWidth || size.Z > MaxBakeWidth)
        {
            return false;
        }

        NavigationServer3D.BakeFromSourceGeometryData(mesh, source);

        if (mesh.GetPolygonCount() == 0)
        {
            return false;
        }

        if (!_map.IsValid)
        {
            _map = NavigationServer3D.MapCreate();
            NavigationServer3D.MapSetActive(_map, true);
            _region = NavigationServer3D.RegionCreate();
            NavigationServer3D.RegionSetMap(_region, _map);
        }

        NavigationServer3D.MapSetCellSize(_map, mesh.CellSize);
        NavigationServer3D.MapSetCellHeight(_map, mesh.CellHeight);
        NavigationServer3D.RegionSetNavigationMesh(_region, mesh);
        _meshIteration = NavigationServer3D.MapGetIterationId(_map);
        return true;
    }

    // Doors are triggers, not walls, so the bake would path straight through one and the
    // door would take the bot elsewhere. Each goes in as a solid block, deeper than the
    // trigger (a path along a row of shop fronts keeps clear of their doors, which the
    // steering's cut corners would brush); a walk that means to go through one (EnterZone)
    // walks on past the path's end into it.
    private static void AddDoors(Zone zone, NavigationMeshSourceGeometryData3D source)
    {
        Node? doors = zone.GetNodeOrNull("Doors");

        if (doors == null)
        {
            return;
        }

        foreach (Node door in doors.GetChildren())
        {
            CollisionShape3D? shape = door.GetNodeOrNull<CollisionShape3D>("Shape");
            BoxShape3D? box = shape?.Shape as BoxShape3D;

            if (shape != null && box != null)
            {
                source.AddFaces(BoxFaces(box.Size + DoorClearance), shape.GlobalTransform);
            }
        }
    }

    // A box's twelve triangles, as plain geometry: a BoxMesh would be a render mesh, read
    // back from the GPU at the bake.
    private static Vector3[] BoxFaces(Vector3 size)
    {
        Vector3 h = size / 2;
        Vector3[] c =
        {
            new Vector3(-h.X, -h.Y, -h.Z), new Vector3(h.X, -h.Y, -h.Z), new Vector3(h.X, -h.Y, h.Z), new Vector3(-h.X, -h.Y, h.Z),
            new Vector3(-h.X, h.Y, -h.Z), new Vector3(h.X, h.Y, -h.Z), new Vector3(h.X, h.Y, h.Z), new Vector3(-h.X, h.Y, h.Z),
        };
        int[] quads = { 0, 1, 2, 3, 4, 7, 6, 5, 0, 4, 5, 1, 1, 5, 6, 2, 2, 6, 7, 3, 3, 7, 4, 0 };
        Vector3[] faces = new Vector3[36];

        for (int q = 0; q < 6; q++)
        {
            int a = quads[q * 4];
            int b = quads[(q * 4) + 1];
            int d = quads[(q * 4) + 2];
            int e = quads[(q * 4) + 3];
            faces[q * 6] = c[a];
            faces[(q * 6) + 1] = c[b];
            faces[(q * 6) + 2] = c[d];
            faces[(q * 6) + 3] = c[a];
            faces[(q * 6) + 4] = c[d];
            faces[(q * 6) + 5] = c[e];
        }

        return faces;
    }

    private static float Flat(Vector3 a, Vector3 b)
    {
        return new Vector2(a.X - b.X, a.Z - b.Z).Length();
    }
}
