namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using MmoGame3d.Data;
using MmoGame3d.Data.Gardening;
using MmoGame3d.Gardening;
using MmoGame3d.Networking;
using MmoGame3d.Rules.Chat;
using MmoGame3d.Rules.Gardening;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Skills;
using MmoGame3d.Rules.Social;
using MmoGame3d.Zones;

/// <summary>
/// The greenhouse. A player makes a house plant at the potting table; the finished
/// design comes here, is checked (known parts, at most five, all on the soil) and
/// becomes a unique plant with provenance, kept forever. The player gets a small
/// ready-made plant and Gardening experience. The newest twelve plants stand outside
/// the greenhouse, where anyone can inspect them; none can be picked up.
/// </summary>
public class ServerGarden
{
    // Tells the achievements when one is earned here; set by ServerGame.
    public Action<Session, string>? Achieved { get; set; }

    // What happens here, for the terminal's status board; set by ServerGame.
    public Action<string>? Post { get; set; }

    public const int OnDisplay = 12;

    private const float ReachSlack = 1.5f;
    private static readonly PackedScene PlantScene = GD.Load<PackedScene>("res://game/gardening/DisplayPlant.tscn");

    private readonly GardenNetwork _network;
    private readonly Network _session;
    private readonly ServerIntents _intents;
    private readonly PersistenceWorker _worker;
    private readonly PlantStore _store;
    private readonly ServerProgress _progress;
    private readonly VisibilityGate _gate;
    private readonly Zone _outside;
    private readonly Action<Session> _bagChanged;
    private readonly ChatFilterPipeline _filters = ChatFilterPipeline.Default();
    private readonly Random _random = new Random();

    public ServerGarden(GardenNetwork network, Network session, ServerIntents intents, PersistenceWorker worker, PlantStore store, ServerProgress progress, VisibilityGate gate, Zone outside, Action<Session> bagChanged)
    {
        _network = network;
        _session = session;
        _intents = intents;
        _worker = worker;
        _store = store;
        _progress = progress;
        _gate = gate;
        _outside = outside;
        _bagChanged = bagChanged;
    }

    // What stands on display when the server starts.
    public void Load()
    {
        _worker.Enqueue(
            () => _store.Newest(OnDisplay),
            plants =>
            {
                foreach (PlantRecord plant in plants)
                {
                    Show(plant.Id, plant.Name, plant.CreatorName, plant.Design);
                }

                GD.Print("Greenhouse: " + plants.Count + " house plants on display");
            },
            e => GD.PrintErr("Loading the house plants failed: " + e.Message));
    }

    public void Open(Session session, PottingTable table)
    {
        session.OpenPottingTable = table;
        _network.SendGardenOpened(session.PeerId);
    }

    public void Complete(Session session, uint intentId, string designText, string name)
    {
        _intents.Run(session, intentId, () =>
        {
            PottingTable? table = session.OpenPottingTable;

            if (table == null || !GodotObject.IsInstanceValid(table) || session.Body == null || !table.IsInReach(session.Body.GlobalPosition, ReachSlack))
            {
                return "You need to be at the potting table.";
            }

            PlantDesign? design = PlantDesign.Parse(designText);

            if (design == null)
            {
                return "That plant cannot be made: use one pot and one to five pieces, all on the soil.";
            }

            string cleanName = name.Length > PlantDesign.MaxNameLength * 2 ? "" : _filters.Apply(name.Trim());

            if (cleanName.Length > PlantDesign.MaxNameLength)
            {
                cleanName = cleanName.Substring(0, PlantDesign.MaxNameLength);
            }

            Save(session, design.Format(), cleanName);
            return "";
        });
    }

    // A player walked up to a plant on display: its provenance card.
    public void Inspect(Session session, DisplayPlant plant)
    {
        long id = plant.PlantId;
        long peer = session.PeerId;
        _worker.Enqueue(
            () => _store.Get(id),
            record =>
            {
                if (record == null)
                {
                    return;
                }

                List<string> history = new List<string>();

                foreach (PlantEvent entry in record.History)
                {
                    history.Add(entry.At.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + "  " + entry.Text);
                }

                _network.SendCard(peer, record.Id, record.Name, record.CreatorName, record.CreatedAt.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture), record.Design, history.ToArray());
            },
            e => GD.PrintErr("Reading house plant " + id + " failed: " + e.Message));
    }

    // Written first; the reward, the display and the message follow once it is kept, so
    // nobody is rewarded for a plant the database never saw.
    private void Save(Session session, string design, string name)
    {
        Guid creatorId = session.Record!.PlayerId;
        string creator = session.Record.DisplayName;
        long peer = session.PeerId;

        _worker.Enqueue(
            () => _store.Create(creatorId, creator, name, design, "Created by " + creator + " at the greenhouse potting table"),
            id =>
            {
                ItemType reward = GardenRewards.SmallPlants[_random.Next(GardenRewards.SmallPlants.Length)];
                session.Inventory!.Add(reward, ItemTier.Standard, 1);
                _bagChanged(session);
                _progress.Award(session, SkillId.Gardening, GardenRewards.GardeningXp);
                session.Body?.Show(Gestures.Work);

                long? leaving = Show(id, name, creator, design);
                _worker.Enqueue(() => _store.AddEvent(id, "Put on display outside the greenhouse, in the outskirts"), e => GD.PrintErr(e.Message));

                if (leaving.HasValue)
                {
                    long gone = leaving.Value;
                    _worker.Enqueue(() => _store.AddEvent(gone, "Taken off display to make room for newer plants"), e => GD.PrintErr(e.Message));
                }

                GD.Print(creator + " made house plant #" + id + (name.Length > 0 ? " \"" + name + "\"" : ""));
                Achieved?.Invoke(session, Rules.Achievements.Achievements.HousePlant);
                Post?.Invoke(creator + " made house plant #" + id + (name.Length > 0 ? ", \"" + name + "\"" : "") + ". It stands outside the greenhouse.");
                _network.SendPlantMade(peer, id, name, ItemCatalog.Describe(reward, ItemTier.Standard));
            },
            e =>
            {
                GD.PrintErr("Saving a house plant failed: " + e.Message);
                _session.SendNotice(peer, "The greenhouse could not keep your plant. Try again.");
            });
    }

    // A plant takes spot (number mod 12), so each new plant replaces the one twelve
    // before it. Returns the number of the plant it replaced, or null.
    private long? Show(long id, string name, string creator, string design)
    {
        // Among the zone's interactables, where a player's F finds them.
        Node3D plants = _outside.GetNode<Node3D>(MmoGame3d.Interact.Interactable.ParentName);
        int spot = (int)(id % OnDisplay);
        string spotName = "Spot" + spot;
        long? replaced = null;
        DisplayPlant? old = plants.GetNodeOrNull<DisplayPlant>(spotName);

        if (old != null)
        {
            replaced = old.PlantId;
            plants.RemoveChild(old);
            old.QueueFree();
        }

        DisplayPlant plant = PlantScene.Instantiate<DisplayPlant>();
        plant.Name = spotName;
        plant.PlantId = id;
        plant.PlantName = name;
        plant.CreatorName = creator;
        plant.Design = design;
        plant.Position = SpotPosition(spot);
        _gate.Watch(plant.Synchronizer!, _outside.ZoneId);
        plants.AddChild(plant, true);
        return replaced;
    }

    // Two rows of six in front of the greenhouse. Placeholder layout.
    private static Vector3 SpotPosition(int spot)
    {
        int row = spot / 6;
        int column = spot % 6;
        return new Vector3(17.5f + (column * 2.6f), 0f, 30f + (row * 2.6f));
    }
}
