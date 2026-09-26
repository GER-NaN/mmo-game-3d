namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Items;
using MmoGame3d.Players;
using MmoGame3d.Rules.Items;
using MmoGame3d.Zones;

/// <summary>
/// The items lying in the zones: stocks each zone at start, puts one back every few
/// seconds while a zone is below its stock, and hands a touched item to whoever walked
/// into it. Ground items are not saved: each server start lays out a fresh set.
/// </summary>
public class GroundItems
{
    // One back per interval, not all at once: a street cleared of items fills in
    // slowly rather than snapping back.
    private const double RefillSeconds = 8;
    private const int SpotAttempts = 20;

    private static readonly PackedScene ItemScene = GD.Load<PackedScene>("res://game/items/GroundItem.tscn");

    private readonly VisibilityGate _gate;
    private readonly Action<Player, GroundItem> _pickedUp;
    private readonly LootTable _loot = LootTable.Ground();
    private readonly Random _random = new Random();
    private readonly List<Zone> _zones = new List<Zone>();
    private double _sinceRefill;

    public GroundItems(VisibilityGate gate, Action<Player, GroundItem> pickedUp)
    {
        _gate = gate;
        _pickedUp = pickedUp;
    }

    public void Stock(Zone zone)
    {
        _zones.Add(zone);

        for (int i = 0; i < zone.ItemStock; i++)
        {
            SpawnOne(zone);
        }
    }

    public void Tick(double delta)
    {
        _sinceRefill += delta;

        if (_sinceRefill < RefillSeconds)
        {
            return;
        }

        _sinceRefill = 0;

        foreach (Zone zone in _zones)
        {
            if (zone.Items.GetChildCount() < zone.ItemStock)
            {
                SpawnOne(zone);
            }
        }
    }

    // Something a player dropped: it lies where it fell until someone picks it up. The
    // zone's refill counts it like any other item on the ground.
    public void DropAt(Zone zone, Vector3 spot, ItemType type, ItemTier tier, int quantity)
    {
        GroundItem item = ItemScene.Instantiate<GroundItem>();
        item.Name = Guid.NewGuid().ToString("N");
        item.TypeId = (int)type;
        item.TierId = (int)tier;
        item.Quantity = quantity;
        item.Position = spot;
        item.Touched += OnTouched;

        _gate.Watch(item.Synchronizer, zone.ZoneId);
        zone.Items.AddChild(item, true);
    }

    private void SpawnOne(Zone zone)
    {
        Vector3 spot;

        if (!TryFindSpot(zone, out spot))
        {
            GD.PrintErr("Zone " + zone.ZoneId + " has no free spot for an item; is it full?");
            return;
        }

        LootRoll roll = _loot.Roll(_random);
        GroundItem item = ItemScene.Instantiate<GroundItem>();

        // Node names are unique among siblings and the same on every client.
        item.Name = Guid.NewGuid().ToString("N");
        item.TypeId = (int)roll.Type;
        item.TierId = (int)roll.Tier;
        item.Quantity = roll.Quantity;
        item.Position = spot;
        item.Touched += OnTouched;

        _gate.Watch(item.Synchronizer, zone.ZoneId);
        zone.Items.AddChild(item, true);
    }

    private void OnTouched(GroundItem item, Player player)
    {
        item.MarkTaken();
        _pickedUp(player, item);
        item.QueueFree();
    }

    private bool TryFindSpot(Zone zone, out Vector3 spot)
    {
        Vector2 half = zone.ItemAreaSize / 2f;

        for (int attempt = 0; attempt < SpotAttempts; attempt++)
        {
            Vector3 candidate = new Vector3(
                (float)((_random.NextDouble() * 2.0) - 1.0) * half.X,
                0f,
                (float)((_random.NextDouble() * 2.0) - 1.0) * half.Y);

            if (SpaceQueries.IsFree(zone, zone.ToGlobal(candidate)))
            {
                spot = candidate;
                return true;
            }
        }

        spot = Vector3.Zero;
        return false;
    }
}
