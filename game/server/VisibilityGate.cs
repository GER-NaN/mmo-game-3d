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

    // Call before the node is added to the tree, so its first spawn already obeys the
    // filter.
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
    }

    // After someone enters or leaves the zone: every node there asks its filter again.
    public void Refresh(string zoneId)
    {
        List<MultiplayerSynchronizer>? watched;

        if (!_byZone.TryGetValue(zoneId, out watched))
        {
            return;
        }

        foreach (MultiplayerSynchronizer synchronizer in watched)
        {
            synchronizer.UpdateVisibility(0);
        }
    }
}
