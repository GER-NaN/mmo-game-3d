namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Data.Players;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.World;
using MmoGame3d.Zones;

/// <summary>
/// Dev test scenarios: a client started with --scenario names one before it logs in, and
/// a server started with --dev-scenarios sets the player up for it: where they stand,
/// what they carry, what is going on around them. The client's ScenarioDriver then tests
/// the feature at once, through input. Without the server flag every request is ignored.
/// </summary>
public class ServerScenarios
{
    // Short Agent Defense runs for tests, in ms.
    private const int ShortDefenseRun = 8000;

    private readonly bool _enabled;
    private readonly World _world;
    private readonly Dictionary<long, string> _asked = new Dictionary<long, string>();

    public ServerScenarios(bool enabled, World world)
    {
        _enabled = enabled;
        _world = world;
    }

    // Set by ServerGame: the parts a scenario reaches into.
    public ServerTown? Town { get; set; }
    public ServerDefense? Defense { get; set; }
    public ServerDrones? Drones { get; set; }
    public ServerRides? Rides { get; set; }
    public ServerWorldEvents? Events { get; set; }

    public void Ask(long peer, string name)
    {
        if (!_enabled)
        {
            GD.PrintErr("A client asked for dev scenario \"" + name + "\"; this server was not started with --dev-scenarios.");
            return;
        }

        _asked[peer] = name;
    }

    public void Forget(long peer)
    {
        _asked.Remove(peer);
    }

    // At login, once the player is loaded and before the body spawns.
    public void Apply(Session session, PlayerRecord record)
    {
        string? name;

        if (!_asked.TryGetValue(session.PeerId, out name))
        {
            return;
        }

        session.Dollars = Math.Max(session.Dollars, 100);

        switch (name)
        {
            case "cracker":
            case "cameras":
                StandBy(record, ZoneIds.Town, "LibraryTerminal", new Vector3(0f, 0f, 1.3f));
                break;
            case "rootkit":
                StandBy(record, ZoneIds.Town, "LibraryTerminal", new Vector3(0f, 0f, 1.3f));
                Town?.DevRootkitJob(record.PlayerId);
                break;
            case "defense":
                StandBy(record, ZoneIds.Town, "LibraryTerminal", new Vector3(0f, 0f, 1.3f));
                Defense?.UseShortRuns(session, ShortDefenseRun);
                break;
            case "subway":
                StandBy(record, ZoneIds.Subway, "SubwayWall", new Vector3(0f, 0f, 1.6f));
                break;
            case "book":
                StandBy(record, ZoneIds.Subway, "VisitorBook", new Vector3(0f, 0f, 1.2f));
                break;
            case "college":
                // Outside, facing the door: the test walks in.
                StandBy(record, ZoneIds.Town, "../Doors/ToCollege", new Vector3(0f, 0f, 3f));
                record.Yaw = 0f;
                break;
            case "meadows":
                // West of the door at the east end of Main Street, facing it (+x).
                StandBy(record, ZoneIds.Town, "../Doors/ToMeadows", new Vector3(-3f, 0f, 0f));
                record.Yaw = -Mathf.Pi / 2f;
                break;
            case "registrar":
                StandBy(record, ZoneIds.College, "Registrar", new Vector3(0f, 0f, 1.3f));
                break;
            case "taxi-ride":
                // The whole ride, there and back, cut to a few seconds.
                StandBy(record, ZoneIds.Town, "TaxiStand", new Vector3(0f, 0f, 1.3f));
                Town?.DevCleanTaxis();
                Rides?.UseShortRides(session);
                break;
            case "taxi-relog":
                // Saved during a ride whose cabin is long gone (or is someone else's now).
                record.Zone = ZoneIds.Instance(ZoneIds.Taxi, 999);
                record.PositionX = 0f;
                record.PositionY = 0f;
                record.PositionZ = 0f;
                break;
            case "hills":
                StandAtFootOfHill(record);
                break;
            case "swarm":
                // At the swarm's spot with an EMP worn, and a swarm of two on its way.
                StandAtSwarm(record);
                WearEmp(session);
                EquipPhone(session);
                Events?.StartSoon("drone-swarm-meadows", 2);
                break;
            case "lights":
                StandBy(record, ZoneIds.Town, "JunctionBox", new Vector3(0f, 0f, 1.3f));
                Town?.DevLightsJob(record.PlayerId);
                session.Inventory!.Add(ItemType.RamStick, ItemTier.Standard, 1);
                break;
            case "taxi":
                StandBy(record, ZoneIds.Town, "TaxiStand", new Vector3(0f, 0f, 1.3f));
                Town?.DevCleanTaxis();
                break;
            case "fix":
                StandBy(record, ZoneIds.Town, "Signal0", new Vector3(0f, 0f, -1.3f));
                _world.GetZone(ZoneIds.Town)!.GetNode<Town.Fixable>("Interactables/Signal0").Broken = true;
                break;
            case "shop":
                StandBy(record, ZoneIds.Shop, "Shopkeeper", new Vector3(0f, 0f, 1.3f));
                break;
            case "too-dear":
                // Nothing in the pocket: every offer is out of reach.
                StandBy(record, ZoneIds.Shop, "Shopkeeper", new Vector3(0f, 0f, 1.3f));
                session.Dollars = 0;
                break;
            case "phone-dead":
                EquipPhone(session, 0f);
                break;
            case "drop-wall":
                // Up against the front of a north-side building, facing it (-z), with
                // something to drop.
                record.Zone = ZoneIds.Town;
                record.PositionX = -26f;
                record.PositionY = 0f;
                record.PositionZ = -7f;
                record.Yaw = 0f;
                session.Inventory!.Add(ItemType.RamStick, ItemTier.Standard, 1);
                break;
            case "door-exit":
                // Inside the shop where players arrive, facing its door out (+z).
                StandBy(record, ZoneIds.Shop, "../Arrivals/FromTown", Vector3.Zero);
                record.Yaw = Mathf.Pi;
                break;
            case "gap":
                // Behind the north side's buildings, at the back of the 1 m gap between two
                // of them, facing into it (+z) a little askew, as the wedger bot got in.
                record.Zone = ZoneIds.Town;
                record.PositionX = -20.2f;
                record.PositionY = 0f;
                record.PositionZ = -20f;
                record.Yaw = Mathf.Pi - 0.3f;
                break;
            case "garden":
                StandBy(record, ZoneIds.Greenhouse, "PottingTable", new Vector3(0f, 0f, 1.5f));
                break;
            case "load-phone":
            case "load-defense":
                // Anywhere in town, spread out, with the phone equipped and charged.
                Spread(record, ZoneIds.Town, 30f);
                EquipPhone(session);

                if (name == "load-defense")
                {
                    Defense?.UseShortRuns(session, ShortDefenseRun);
                }

                break;
            case "load-taxi":
                StandBy(record, ZoneIds.Town, "TaxiStand", new Vector3(_random.RandfRange(-1.5f, 1.5f), 0f, _random.RandfRange(0.6f, 2f)));
                Town?.DevCleanTaxis();
                break;
            case "load-chat":
                Spread(record, ZoneIds.Town, 30f);
                break;
            case "workbench":
                StandBy(record, ZoneIds.Shop, "Workbench", new Vector3(0f, 0f, 1.3f));
                session.Inventory!.Add(ItemType.Battery, ItemTier.Standard, 1);
                break;
            default:
                GD.PrintErr("No dev scenario \"" + name + "\".");
                return;
        }

        if (name == "cameras")
        {
            // Out of zapping range of the terminal, in view of the cameras, and away from
            // the other scenarios' spots (the college door is at x 15).
            Drones?.SpawnPair(new Vector3(-14f, 0f, -2f));
        }

        GD.Print("Dev scenario \"" + name + "\" set up for " + record.DisplayName);
    }

    // In reach of the thing: the offset if that spot is free, else the first free spot on
    // a ring round it, so furniture beside it does not push the body out of reach.
    private readonly RandomNumberGenerator _random = new RandomNumberGenerator();

    private void Spread(PlayerRecord record, string zoneId, float size)
    {
        record.Zone = zoneId;
        record.PositionX = _random.RandfRange(-size, size);
        record.PositionY = 0f;
        record.PositionZ = _random.RandfRange(-4f, 4f);
    }

    private static void EquipPhone(Session session, float charge = 1f)
    {
        Belongings mine = new Belongings(session.Inventory!, session.Instances);

        foreach (ItemInstance item in session.Instances)
        {
            if (item.Type == ItemType.Phone)
            {
                if (mine.Equipped(SlotType.Device) == null)
                {
                    mine.Equip(item.Id);
                }

                ItemInstance? battery = mine.Inside(item, SlotType.Battery);

                if (battery != null)
                {
                    battery.Charge = charge;
                }

                return;
            }
        }
    }

    // Facing east at the foot of the first rise along the meadows' middle steep enough
    // that a walk up it climbs faster than a jump starts: the old animation took that for
    // a jump. Found, not set, so sculpting the meadows does not break the test.
    private void StandAtFootOfHill(PlayerRecord record)
    {
        const float Look = 5f;
        const float Steep = 0.35f;
        Zone zone = _world.GetZone(ZoneIds.Meadows)!;
        Vector3 at = new Vector3(-1450f, 0f, 0f);

        for (float x = -1450f; x < 1400f; x += Look)
        {
            float? here = SpaceQueries.GroundUnder(zone, new Vector3(x, 0f, 0f), 300f, 600f);
            float? ahead = SpaceQueries.GroundUnder(zone, new Vector3(x + Look, 0f, 0f), 300f, 600f);
            float? further = SpaceQueries.GroundUnder(zone, new Vector3(x + (Look * 2f), 0f, 0f), 300f, 600f);

            if (here != null && ahead != null && further != null && ahead - here > Steep * Look && further - ahead > Steep * Look)
            {
                at = new Vector3(x, here.Value, 0f);
                break;
            }
        }

        record.Zone = ZoneIds.Meadows;
        record.PositionX = at.X;
        record.PositionY = at.Y;
        record.PositionZ = at.Z;
        record.Yaw = -Mathf.Pi / 2f;
    }

    private void StandAtSwarm(PlayerRecord record)
    {
        Zone zone = _world.GetZone(ZoneIds.Meadows)!;
        Vector3 at = zone.GetNode<Node3D>(ServerWorldEvents.SpotsNode + "/DroneSwarm").Position;
        at.Y = SpaceQueries.GroundUnder(zone, at, 300f, 600f) ?? at.Y;
        record.Zone = ZoneIds.Meadows;
        record.PositionX = at.X;
        record.PositionY = at.Y;
        record.PositionZ = at.Z;
    }

    private static void WearEmp(Session session)
    {
        ItemInstance emp = new ItemInstance(Guid.NewGuid(), ItemType.EmpEmitter, ItemTier.Standard);
        session.Instances.Add(emp);
        new Belongings(session.Inventory!, session.Instances).Equip(emp.Id);
    }

    private void StandBy(PlayerRecord record, string zoneId, string thing, Vector3 offset)
    {
        Zone zone = _world.GetZone(zoneId)!;
        Node3D target = zone.GetNode<Node3D>("Interactables/" + thing);
        Vector3 at = target.Position + offset;

        for (int i = 0; i < 12 && !SpaceQueries.IsFree(zone, zone.ToGlobal(at)); i++)
        {
            float angle = Mathf.Tau * i / 12f;
            at = target.Position + (new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 1.4f);
        }

        record.Zone = zoneId;
        record.PositionX = at.X;
        record.PositionY = at.Y;
        record.PositionZ = at.Z;
    }
}
