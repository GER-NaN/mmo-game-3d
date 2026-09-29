namespace MmoGame3d.Bots;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Client;
using MmoGame3d.Players;
using MmoGame3d.Rules.Maps;
using MmoGame3d.Zones;

/// <summary>
/// Walks the zone it is in until its map is all discovered, as the map counts it: cells
/// of Discovery.CellSize, each discovered once the player has been within
/// Discovery.SightRadius of it (the server tells the client). It walks toward the nearest
/// cell still dark, and picks again as cells light up. A cell it cannot light (its walk
/// ends, or it stops moving) is passed over; the step fails at the end with those cells
/// named, since a player could not light them either. Works in any zone with a map. If a
/// door takes it out of the zone, it goes back and surveys on.
/// </summary>
public class SurveyStep : BotStep
{
    private const double LookInterval = 0.25;

    // Standing still this long on the way to a cell gives it up.
    private const double StuckSeconds = 8;
    private const float StuckDistance = 1f;

    // After the walk ends, the server's word on the cell may still be on its way.
    private const double ArrivedGrace = 1.5;

    private readonly HashSet<int> _passedOver = new HashSet<int>();
    private string _zoneId = "";
    private int _target = -1;
    private double _sinceLook = LookInterval;
    private Vector3 _lastPlace;
    private double _still;
    private double _arrivedFor;
    private int _reportedTenths = -1;

    public SurveyStep()
        : base("survey", 900)
    {
    }

    public override void Start(BotBody body)
    {
        _zoneId = body.Zone?.ZoneId ?? "";
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        if (_target >= 0)
        {
            body.Navigator.Tick(body, delta);
        }

        _sinceLook += delta;

        if (_sinceLook < LookInterval)
        {
            return BotStepState.Running;
        }

        double looked = _sinceLook;
        _sinceLook = 0;
        Zone? zone = body.Zone;
        ClientView? view = body.View;
        Player? player = body.Player;

        if (zone == null || view == null || player == null)
        {
            return BotStepState.Running;
        }

        if (zone.ZoneId != _zoneId)
        {
            body.Events.Write("survey", "a door took it to " + zone.ZoneId + "; going back");
            body.InsertNext(new List<BotStep> { new EnterZoneStep(_zoneId, false), new SurveyStep() });
            return BotStepState.Done;
        }

        if (zone.MapSize == Vector2.Zero)
        {
            return Fail(_zoneId + " has no map");
        }

        Discovery map = new Discovery(zone.MapSize.X, zone.MapSize.Y);
        map.Load(view.MapCells);
        int cells = map.Columns * map.Rows;
        int found = map.DiscoveredCount();
        Report(body, found, cells);

        if (found == cells)
        {
            return BotStepState.Done;
        }

        if (_target >= 0 && map.IsDiscovered(_target % map.Columns, _target / map.Columns))
        {
            _target = -1;
        }

        if (_target >= 0 && GivenUp(body, player, looked))
        {
            _passedOver.Add(_target);
            body.Events.Write("survey", "passed over the cell at " + CellName(map, _target));
            _target = -1;
        }

        if (_target < 0)
        {
            _target = Nearest(map, zone.ToLocal(player.GlobalPosition));

            if (_target < 0)
            {
                return Fail(Percent(found, cells) + " of " + _zoneId + "; not reached: " + string.Join(", ", Names(map)));
            }

            body.Navigator.Go(zone, zone.ToGlobal(Centre(map, _target)), false);
            _lastPlace = player.GlobalPosition;
            _still = 0;
            _arrivedFor = 0;
        }

        return BotStepState.Running;
    }

    public override void End(BotBody body)
    {
        body.Navigator.Stop(body);
    }

    // The walk ended with the cell still dark, or the body has not moved for a while.
    private bool GivenUp(BotBody body, Player player, double looked)
    {
        _arrivedFor = body.Navigator.Arrived ? _arrivedFor + looked : 0;

        if (player.GlobalPosition.DistanceTo(_lastPlace) >= StuckDistance)
        {
            _lastPlace = player.GlobalPosition;
            _still = 0;
        }
        else
        {
            _still += looked;
        }

        return _arrivedFor >= ArrivedGrace || _still >= StuckSeconds;
    }

    // The coverage, each time it passes another tenth.
    private void Report(BotBody body, int found, int cells)
    {
        int tenths = found * 10 / cells;

        if (tenths != _reportedTenths)
        {
            _reportedTenths = tenths;
            body.Events.Write("coverage", Percent(found, cells) + " (" + found + " of " + cells + " cells)");
        }
    }

    // The dark cell nearest the player that has not been passed over, or -1.
    private int Nearest(Discovery map, Vector3 local)
    {
        int best = -1;
        float bestDistance = float.MaxValue;

        for (int cell = 0; cell < map.Columns * map.Rows; cell++)
        {
            if (map.IsDiscovered(cell % map.Columns, cell / map.Columns) || _passedOver.Contains(cell))
            {
                continue;
            }

            Vector3 centre = Centre(map, cell);
            float distance = new Vector2(centre.X - local.X, centre.Z - local.Z).Length();

            if (distance < bestDistance)
            {
                best = cell;
                bestDistance = distance;
            }
        }

        return best;
    }

    private List<string> Names(Discovery map)
    {
        List<string> names = new List<string>();

        foreach (int cell in _passedOver)
        {
            names.Add(CellName(map, cell));
        }

        return names;
    }

    // Zone-local, as Discovery measures: the map's box is centred on the zone's origin.
    private static Vector3 Centre(Discovery map, int cell)
    {
        float x = (-map.Width / 2f) + (((cell % map.Columns) + 0.5f) * Discovery.CellSize);
        float z = (-map.Depth / 2f) + (((cell / map.Columns) + 0.5f) * Discovery.CellSize);
        return new Vector3(x, 0, z);
    }

    private static string CellName(Discovery map, int cell)
    {
        Vector3 centre = Centre(map, cell);
        return "(" + centre.X + ", " + centre.Z + ")";
    }

    private static string Percent(int found, int cells)
    {
        return (found * 100 / cells) + "%";
    }
}
