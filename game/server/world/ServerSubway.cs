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
/// The subway under Old Town: spraying your name on the wall, once, for good, and the
/// visitor book with every name. The newest tags are kept here and put on the wall's
/// synced text; the book reads the database a page at a time.
/// </summary>
public class ServerSubway
{
    // Tells the achievements when one is earned here; set by ServerGame.
    public Action<Session, string>? Achieved { get; set; }

    private readonly SubwayNetwork _network;
    private readonly Network _session;
    private readonly PersistenceWorker _worker;
    private readonly SubwayStore _store;
    private readonly Zone _zone;
    private readonly List<SubwayTag> _shown = new List<SubwayTag>();
    private readonly Random _random = new Random();

    public ServerSubway(SubwayNetwork network, Network session, PersistenceWorker worker, SubwayStore store, VisibilityGate gate, Zone zone)
    {
        _network = network;
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
        get { return _zone.GetNode<SubwayWallNode>("Interactables/SubwayWall"); }
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

    // Page 0 is the first names ever sprayed; a page past the end shows the last one.
    public void ReadBook(Session session, int page)
    {
        if (session.ZoneId != _zone.ZoneId)
        {
            return;
        }

        long peer = session.PeerId;
        _worker.Enqueue(
            () =>
            {
                int count = _store.Count(SubwayWall.OldTown);
                int pages = Math.Max(1, (count + SubwayWall.BookPage - 1) / SubwayWall.BookPage);
                int shown = Math.Clamp(page, 0, pages - 1);
                List<string> lines = new List<string>();

                foreach (SubwayTag tag in _store.Page(SubwayWall.OldTown, shown * SubwayWall.BookPage, SubwayWall.BookPage))
                {
                    lines.Add("#" + tag.Id + "   " + tag.Name);
                }

                return new object[] { shown, pages, lines.ToArray() };
            },
            result => _network.SendPage(peer, (int)result[0], (int)result[1], (string[])result[2]),
            e => GD.PrintErr("Reading the visitor book failed: " + e.Message));
    }
}
