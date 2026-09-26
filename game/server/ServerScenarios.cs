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

    private static void EquipPhone(Session session)
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
                    battery.Charge = 1f;
                }

                return;
            }
        }
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
