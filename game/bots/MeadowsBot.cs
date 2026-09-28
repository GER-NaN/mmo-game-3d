namespace MmoGame3d.Bots;

using System.Collections.Generic;
using System.IO;
using Godot;
using MmoGame3d.Players;
using MmoGame3d.Zones;

/// <summary>
/// A bot that walks to the meadows through the doors, then walks forward a few seconds
/// there. From town it takes the meadows door; from any other zone, the door to town
/// first. Starting in the meadows, it goes to town and back, so the visit is a real one.
/// Inside a zone it follows a path over a navigation mesh baked from the zone's
/// collision when it arrives, and walks straight where no mesh bakes (terrain).
/// </summary>
public partial class MeadowsBot : Node
{
    private const double LookInterval = 0.25;
    private const string Meadows = "meadows";
    private const string Town = "town";

    private const double SettleSeconds = 1;
    private const double WalkForwardSeconds = 3;
    private const float MinWalked = 3f;

    // Past this width the engine refuses to bake at the default cell size, with an error.
    private const float MaxBakeWidth = 300f;

    // Close enough to a path point to aim at the next one.
    private const float PointReach = 0.75f;

    // Facing within this of the aim, it walks; past it, it only turns.
    private const float WalkWithin = 0.7f;
    private const float TurnDeadZone = 0.08f;

    private readonly HashSet<string> _held = new HashSet<string>();

    private string _folder = "";
    private BotEventLog? _events;
    private double _sinceLook;
    private Stage _stage = Stage.WaitForWorld;
    private double _stageTime;
    private Player? _player;
    private string _zoneId = "";
    private bool _beenElsewhere;
    private Vector3 _door;
    private Vector3[] _path = new Vector3[0];
    private int _pathIndex;
    private Vector3 _walkStart;
    private Rid _map;
    private Rid _region;

    // The map's sync count when the mesh was set; a path is asked for once it has moved.
    private long _meshIteration = -1;

    private enum Stage
    {
        WaitForWorld,
        Settle,
        WalkToDoor,
        WalkForward,
        Done,
    }

    public override void _Ready()
    {
        _folder = FolderFromCommandLine();

        if (_folder.Length == 0)
        {
            GD.PushError("MeadowsBot: no --bot-folder given; the bot does nothing.");
            SetProcess(false);
            return;
        }

        _events = new BotEventLog(_folder);
        _events.Write("started", "");
    }

    public override void _Process(double delta)
    {
        _stageTime += delta;

        // Steering needs every frame; everything else is looked at a few times a second.
        // On a zone change the old body leaves the tree before it is freed.
        if (_stage == Stage.WalkToDoor && _path.Length > 0 && _player != null && IsInstanceValid(_player) && _player.IsInsideTree())
        {
            Steer();
        }

        _sinceLook += delta;

        if (_sinceLook < LookInterval)
        {
            return;
        }

        _sinceLook = 0;

        if (File.Exists(Path.Combine(_folder, Bot.StopFile)))
        {
            ReleaseAll();
            _events!.Write("stopped", "");
            _events.Close();
            SetProcess(false);
            GetTree().Quit();
            return;
        }

        // The player's body is made anew in each zone.
        if (_player == null || !IsInstanceValid(_player))
        {
            _player = GetTree().GetFirstNodeInGroup(Player.LocalGroup) as Player;
        }

        Zone? zone = FindZone(GetTree().Root);

        switch (_stage)
        {
            case Stage.WaitForWorld:
                if (_player != null && zone != null)
                {
                    _events!.Write("in-world", zone.ZoneId);
                    Arrive(zone);
                }

                break;
            case Stage.Settle:
                if (_stageTime >= SettleSeconds && _player != null)
                {
                    StartLeg(zone!);
                }

                break;
            case Stage.WalkToDoor:
                if (_path.Length == 0 && NavigationServer3D.MapGetIterationId(_map) > _meshIteration)
                {
                    _path = NavigationServer3D.MapGetPath(_map, _player!.GlobalPosition, _door, true);

                    if (_path.Length == 0)
                    {
                        _path = new Vector3[] { _door };
                    }

                    _events!.Write("walking", "to the door, " + _path.Length + " path points");
                }

                if (zone != null && zone.ZoneId != _zoneId)
                {
                    ReleaseAll();
                    _events!.Write("arrived", zone.ZoneId);
                    Arrive(zone);
                }

                break;
            case Stage.WalkForward:
                if (_stageTime >= WalkForwardSeconds)
                {
                    Hold("move_forward", false);
                    float walked = Flat(_player!.NetPosition, _walkStart);

                    if (walked >= MinWalked)
                    {
                        _events!.Write("walked", walked.ToString("0.0") + " m in the meadows");
                        _events.Write("done", "");
                    }
                    else
                    {
                        _events!.Write("failed", "walked only " + walked.ToString("0.0") + " m");
                    }

                    Next(Stage.Done);
                }

                break;
            case Stage.Done:
                break;
        }
    }

    private void Arrive(Zone zone)
    {
        _zoneId = zone.ZoneId;

        if (_zoneId != Meadows)
        {
            _beenElsewhere = true;
        }

        Next(Stage.Settle);
    }

    // Settled in a zone: either the visit is made, or on to the next door.
    private void StartLeg(Zone zone)
    {
        if (_zoneId == Meadows && _beenElsewhere)
        {
            _walkStart = _player!.NetPosition;
            Hold("move_forward", true);
            _events!.Write("walking", "forward");
            Next(Stage.WalkForward);
            return;
        }

        string toward = _zoneId == Town ? Meadows : Town;
        Door? door = DoorTo(zone, toward);

        if (door == null)
        {
            _events!.Write("failed", "no door from " + _zoneId + " to " + toward);
            Next(Stage.Done);
            return;
        }

        _door = door.GlobalPosition;
        _path = new Vector3[0];
        _pathIndex = 0;

        if (!Bake(zone))
        {
            _path = new Vector3[] { _door };
            _events!.Write("walking", "straight to the " + toward + " door (no mesh bakes here)");
        }

        Next(Stage.WalkToDoor);
    }

    // Toward the next path point: turn with the turn keys, walk with the forward key.
    // Past the path's end it keeps walking at the door until the zone changes.
    private void Steer()
    {
        Vector3 here = _player!.GlobalPosition;

        while (_pathIndex < _path.Length && Flat(here, _path[_pathIndex]) < PointReach)
        {
            _pathIndex++;
        }

        Vector3 aim = _pathIndex < _path.Length ? _path[_pathIndex] : _door;
        Vector3 to = aim - here;
        float wanted = Mathf.Atan2(-to.X, -to.Z);
        float off = Mathf.Wrap(wanted - _player.Heading, -Mathf.Pi, Mathf.Pi);

        Hold("turn_left", off > TurnDeadZone);
        Hold("turn_right", off < -TurnDeadZone);
        Hold("move_forward", Mathf.Abs(off) < WalkWithin);
    }

    // A navigation mesh baked from the zone's static collision, set on this bot's own
    // map. The map takes it at its next sync, after which a path can be asked for. False
    // where nothing bakes: terrain has no collision nodes to read, which leaves only the
    // zone's edge walls, too far apart to bake.
    private bool Bake(Zone zone)
    {
        NavigationMesh mesh = new NavigationMesh();
        // Heights are whole cells (0.25 m), or the engine warns that it rounds them.
        mesh.AgentRadius = 0.5f;
        mesh.AgentHeight = 2f;
        mesh.AgentMaxClimb = 0.5f;
        mesh.GeometryParsedGeometryType = NavigationMesh.ParsedGeometryType.StaticColliders;

        NavigationMeshSourceGeometryData3D source = new NavigationMeshSourceGeometryData3D();
        NavigationServer3D.ParseSourceGeometryData(mesh, source, zone);
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

    private void Next(Stage stage)
    {
        _stage = stage;
        _stageTime = 0;
    }

    // A key held down or let go, as the input system sees a real one.
    private void Hold(string action, bool down)
    {
        if (down && !_held.Contains(action))
        {
            Input.ActionPress(action);
            _held.Add(action);
        }
        else if (!down && _held.Contains(action))
        {
            Input.ActionRelease(action);
            _held.Remove(action);
        }
    }

    private void ReleaseAll()
    {
        foreach (string action in new List<string>(_held))
        {
            Hold(action, false);
        }
    }

    private static float Flat(Vector3 a, Vector3 b)
    {
        return new Vector2(a.X - b.X, a.Z - b.Z).Length();
    }

    private static Door? DoorTo(Zone zone, string targetZone)
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

    private static Zone? FindZone(Node node)
    {
        Zone? zone = node as Zone;

        if (zone != null)
        {
            return zone;
        }

        foreach (Node child in node.GetChildren())
        {
            Zone? found = FindZone(child);

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static string FolderFromCommandLine()
    {
        string[] args = OS.GetCmdlineUserArgs();

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--bot-folder")
            {
                return args[i + 1];
            }
        }

        return "";
    }
}
