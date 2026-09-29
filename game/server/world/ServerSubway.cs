namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Data;
using MmoGame3d.Data.Town;
using MmoGame3d.Networking;
using MmoGame3d.Rules.Social;
using MmoGame3d.Rules.Town;
using MmoGame3d.Subway;
using MmoGame3d.Zones;

/// <summary>
/// The subway under Old Town: spraying your name on the wall, once, for good. The wall
/// is the visitor book. The newest tags are kept here and put on the wall's synced text.
/// </summary>
public class ServerSubway
{
    // Tells the achievements when one is earned here; set by ServerGame.
    public Action<Session, string>? Achieved { get; set; }

    private readonly Network _session;
    private readonly PersistenceWorker _worker;
    private readonly SubwayStore _store;
    private readonly Zone _zone;
    private readonly List<SubwayTag> _shown = new List<SubwayTag>();
    private readonly Random _random = new Random();

    public ServerSubway(Network session, PersistenceWorker worker, SubwayStore store, VisibilityGate gate, Zone zone)
    {
        _session = session;
        _worker = worker;
        _store = store;
        _zone = zone;
        gate.Watch(Wall.Synchronizer!, zone.ZoneId);
    }

    // What happens here, for the terminal's status board; set by ServerGame.
    public Action<string>? Post { get; set; }

    private SubwayWallNode Wall
    {
        get { return _zone.InGroup<SubwayWallNode>(SubwayWallNode.Group)!; }
    }

    public void Load()
    {
        _worker.Enqueue(
            () => _store.Newest(SubwayWall.OldTown, SubwayWall.Shown),
            tags =>
            {
                _shown.AddRange(tags);
                Wall.Tags = SubwayWall.Pack(_shown);
                GD.Print("Subway: " + tags.Count + " names on the wall");
            },
            e => GD.PrintErr("Loading the subway wall failed: " + e.Message));
    }

    public void Spray(Session session)
    {
        Guid playerId = session.Record!.PlayerId;
        string name = session.Record.DisplayName;
        long peer = session.PeerId;
        uint paint = SubwayWall.Paints[_random.Next(SubwayWall.Paints.Length)];
        bool made = false;

        _worker.Enqueue(
            () => _store.Spray(SubwayWall.OldTown, playerId, name, paint, out made),
            tag =>
            {
                if (!made)
                {
                    _session.SendNotice(peer, "Your name is already on the wall, tag #" + tag.Id + ". It stays there for good.");
                    return;
                }

                _shown.Add(tag);

                if (_shown.Count > SubwayWall.Shown)
                {
                    _shown.RemoveAt(0);
                }

                Wall.Tags = SubwayWall.Pack(_shown);
                session.Body?.Show(Gestures.Work);
                _session.SendNotice(peer, "You sprayed your name on the wall, tag #" + tag.Id + ". Nobody can paint over it.");
                Achieved?.Invoke(session, Rules.Achievements.Achievements.SubwayTag);
                Post?.Invoke(name + " sprayed their name on the wall in the Old Town subway.");
            },
            e =>
            {
                GD.PrintErr("Spraying a subway tag failed: " + e.Message);
                _session.SendNotice(peer, "The paint would not stick. Try again.");
            });
    }
}
