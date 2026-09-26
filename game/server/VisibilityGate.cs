namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Decides which clients are sent which synced node: a client sees a node while it is
/// in the world and in the node's zone. Visibility also decides spawning, so a node is
/// created on a client when it becomes visible there and removed when it stops.
///
/// Godot shows a synchronizer to a peer when it is public and every filter agrees, so
/// the filter here is the whole gate. It is asked only when something changes (a player
/// enters or leaves a zone), not every frame: with hundreds of players and items, a
/// per-frame check would be millions of calls into C# a second.
/// </summary>
public class VisibilityGate
{
    private readonly Func<long, string, bool> _canSee;
    private readonly Dictionary<string, List<MultiplayerSynchronizer>> _byZone = new Dictionary<string, List<MultiplayerSynchronizer>>();

    public VisibilityGate(Func<long, string, bool> canSee)
    {
        _canSee = canSee;
    }

    // For a spawned node, call before it is added to the tree, so its first spawn already
    // obeys the filter.
    public void Watch(MultiplayerSynchronizer synchronizer, string zoneId)
    {
        synchronizer.VisibilityUpdateMode = MultiplayerSynchronizer.VisibilityUpdateModeEnum.None;
        synchronizer.AddVisibilityFilter(Callable.From<long, bool>(peer => _canSee(peer, zoneId)));

        List<MultiplayerSynchronizer>? watched;

        if (!_byZone.TryGetValue(zoneId, out watched))
        {
            watched = new List<MultiplayerSynchronizer>();
            _byZone[zoneId] = watched;
        }

        watched.Add(synchronizer);
        synchronizer.TreeExiting += () => watched.Remove(synchronizer);

        // A node already in the tree (a zone's own state, not something spawned) starts out
        // shown to every peer until its filter is first asked; ask now, or a client in
        // another zone is sent state for a node it does not have.
        if (synchronizer.IsInsideTree())
        {
            synchronizer.UpdateVisibility(0);
        }
    }

    // After one player entered or left the zone: every node there asks its filter again,
    // for that player only. Asking for every peer was every node times every player calls
    // into C# per arrival (about 14,000 at 100 players in a zone, 16 ms a join); only the
    // one who moved can see differently.
    public void Refresh(string zoneId, long peer)
    {
        List<MultiplayerSynchronizer>? watched;

        if (!_byZone.TryGetValue(zoneId, out watched))
        {
            return;
        }

        foreach (MultiplayerSynchronizer synchronizer in watched)
        {
            synchronizer.UpdateVisibility((int)peer);
        }
    }
}
