namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using MmoGame3d.Chests;
using MmoGame3d.Networking;
using MmoGame3d.Rules.Items;

/// <summary>
/// Chests: opening one gives whoever opened it what it holds, rolled from the chest's
/// loot table there and then, and it fills again after a while. Chests are not saved: a
/// server start finds every chest full, as in mmo-game.
/// </summary>
public class ServerChests
{
    // Placeholder: long enough that a chest is not a tap, short enough to come back to.
    private static readonly TimeSpan RefillAfter = TimeSpan.FromMinutes(5);

    private readonly Network _session;
    private readonly Action<Session> _bagChanged;
    private readonly LootTable _loot = LootTable.Chest();
    private readonly Random _random = new Random();
    private readonly Dictionary<Chest, DateTime> _emptySince = new Dictionary<Chest, DateTime>();

    public ServerChests(Network session, Action<Session> bagChanged)
    {
        _session = session;
        _bagChanged = bagChanged;
    }

    public void Open(Session session, Chest chest)
    {
        if (!chest.HasItem)
        {
            _session.SendNotice(session.PeerId, "The " + chest.ChestName + " is empty. It fills again in a while.");
            return;
        }

        LootRoll roll = _loot.Roll(_random);
        session.Inventory!.Add(roll.Type, roll.Tier, roll.Quantity);
        chest.HasItem = false;
        _emptySince[chest] = DateTime.UtcNow;
        _bagChanged(session);
        _session.SendNotice(session.PeerId, "Found " + roll.Quantity + " " + ItemCatalog.Describe(roll.Type, roll.Tier) + " in the " + chest.ChestName + ".");
    }

    public void Tick()
    {
        DateTime now = DateTime.UtcNow;
        List<Chest> refilled = new List<Chest>();

        foreach (KeyValuePair<Chest, DateTime> entry in _emptySince)
        {
            if (now - entry.Value >= RefillAfter)
            {
                entry.Key.HasItem = true;
                refilled.Add(entry.Key);
            }
        }

        foreach (Chest chest in refilled)
        {
            _emptySince.Remove(chest);
        }
    }
}
