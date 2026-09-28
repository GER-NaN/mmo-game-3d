namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Data;
using MmoGame3d.Data.Maps;
using MmoGame3d.Networking;
using MmoGame3d.Rules.Maps;
using MmoGame3d.Zones;

/// <summary>
/// The map shows only where a player has been. The server decides that from where the
/// body walks, so a client cannot uncover the map by claiming it went somewhere; it
/// tells the owner each time a cell is found, and saves what changed with the player.
/// </summary>
public class ServerMaps
{
    // Tells the achievements when one is earned here; set by ServerGame.
    public Action<Session, string>? Achieved { get; set; }

    // Walking pace is a few metres a second and a cell is ten, so twice a second is
    // plenty.
    private const double LookIntervalSeconds = 0.5;

    private readonly World _world;
    private readonly Network _network;
    private readonly PersistenceWorker _worker;
    private readonly DiscoveryStore _store;
    private readonly Func<IEnumerable<Session>> _sessions;
    private double _sinceLook;

    public ServerMaps(World world, Network network, PersistenceWorker worker, DiscoveryStore store, Func<IEnumerable<Session>> sessions)
    {
        _world = world;
        _network = network;
        _worker = worker;
        _store = store;
        _sessions = sessions;
    }

    public void Tick(double delta)
    {
        _sinceLook += delta;

        if (_sinceLook < LookIntervalSeconds)
        {
            return;
        }

        _sinceLook = 0;

        foreach (Session session in _sessions())
        {
            if (session.State != SessionState.InWorld || session.Body == null)
            {
                continue;
            }

            Discovery? map = MapOf(session, session.ZoneId!);

            if (map != null && map.See(session.Body.Position.X, session.Body.Position.Z))
            {
                session.UnsavedMaps.Add(session.ZoneId!);
                _network.SendMap(session.PeerId, session.ZoneId!, map.ToBytes());
                Explored(session, session.ZoneId!, map);
            }
        }
    }

    private void Explored(Session session, string zoneId, Discovery map)
    {
        if (!Rules.Achievements.Achievements.IsExplored(map.DiscoveredCount(), map.Columns * map.Rows))
        {
            return;
        }

        switch (zoneId)
        {
            case Rules.World.ZoneIds.Town:
                Achieved?.Invoke(session, Rules.Achievements.Achievements.ExploreOldTown);
                break;
            case Rules.World.ZoneIds.Outskirts:
                Achieved?.Invoke(session, Rules.Achievements.Achievements.ExploreOutskirts);
                break;
        }
    }

    // On arrival in a zone, at login or through a door: the client has no map for it yet.
    public void Send(Session session)
    {
        Discovery? map = MapOf(session, session.ZoneId!);

        if (map != null)
        {
            _network.SendMap(session.PeerId, session.ZoneId!, map.ToBytes());
        }
    }

    // Only the zones changed since the last save, copied now, so the worker never reads a
    // map the game thread is still marking.
    public void Save(Session session)
    {
        Guid playerId = session.Record!.PlayerId;

        foreach (string zoneId in session.UnsavedMaps)
        {
            string zone = zoneId;
            byte[] cells = session.Maps[zoneId].ToBytes();
            _worker.Enqueue(() => _store.Save(playerId, zone, cells), e => GD.PrintErr("Saving the map of " + zone + " failed: " + e.Message));
        }

        session.UnsavedMaps.Clear();
    }

    // Built on first use from what was saved; null for a zone without a map.
    private Discovery? MapOf(Session session, string zoneId)
    {
        Discovery? map;

        if (session.Maps.TryGetValue(zoneId, out map))
        {
            return map;
        }

        Zone? zone = _world.GetZone(zoneId);

        if (zone == null || zone.MapSize == Vector2.Zero)
        {
            return null;
        }

        map = new Discovery(zone.MapSize.X, zone.MapSize.Y);
        byte[]? saved;

        if (session.Record!.Discovered.TryGetValue(zoneId, out saved))
        {
            map.Load(saved);
        }

        session.Maps[zoneId] = map;
        return map;
    }
}
