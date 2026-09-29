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

    // How far above the feet a player sprays.
    private const float SprayHeight = 1.5f;

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
                foreach (SubwayTag tag in tags)
                {
                    if (tag.Place == null)
                    {
                        PlaceOld(tag);
                    }

                    _shown.Add(tag);
                }

                Wall.Tags = SubwayWall.Pack(_shown);
                GD.Print("Subway: " + tags.Count + " names on the wall");
            },
            e => GD.PrintErr("Loading the subway wall failed: " + e.Message));
    }

    // A tag sprayed before places were kept: somewhere free, near a spot picked from its
    // number, and kept there from now on.
    private void PlaceOld(SubwayTag tag)
    {
        Random spot = new Random((int)(tag.Id * 7919 % int.MaxValue));
        float wantX = ((float)spot.NextDouble() - 0.5f) * SubwayWall.Width;
        float wantY = SubwayWall.Low + ((float)spot.NextDouble() * (SubwayWall.High - SubwayWall.Low));
        tag.Place = PlaceNew(tag.Name, spot, wantX, wantY);
        long id = tag.Id;
        TagPlace place = tag.Place;
        _worker.Enqueue(() => _store.Place(id, place), e => GD.PrintErr("Placing a subway tag failed: " + e.Message));
    }

    // The free spot nearest the one wanted, at a random size and slant. On a full wall, the
    // wanted spot, over whatever is there.
    private TagPlace PlaceNew(string name, Random random, float wantX, float wantY)
    {
        int size = SubwayWall.SmallestSize + random.Next(SubwayWall.LargestSize - SubwayWall.SmallestSize + 1);
        float angle = (float)((random.NextDouble() * 2.0) - 1.0) * SubwayWall.MostSlant;
        TagPlace? place = TagPlacement.Find(_shown, name, size, angle, wantX, wantY);

        if (place != null)
        {
            return place;
        }

        float halfWidth = TagPlacement.Width(name, size) / 2f;
        float halfHeight = TagPlacement.Height(size) / 2f;

        return new TagPlace
        {
            X = Math.Clamp(wantX, (-SubwayWall.Width / 2f) + halfWidth, Math.Max((-SubwayWall.Width / 2f) + halfWidth, (SubwayWall.Width / 2f) - halfWidth)),
            Y = Math.Clamp(wantY, SubwayWall.Low + halfHeight, Math.Max(SubwayWall.Low + halfHeight, SubwayWall.High - halfHeight)),
            Angle = angle,
            Size = size,
        };
    }

    public void Spray(Session session)
    {
        Guid playerId = session.Record!.PlayerId;
        string name = session.Record.DisplayName;
        long peer = session.PeerId;
        uint paint = SubwayWall.Paints[_random.Next(SubwayWall.Paints.Length)];
        bool made = false;

        // In front of the player: where they stand along the wall, at about head height.
        Vector3 facing = Wall.ToLocal(session.Body!.GlobalPosition + new Vector3(0f, SprayHeight, 0f));
        TagPlace place = PlaceNew(name, _random, facing.X, facing.Y);

        _worker.Enqueue(
            () =>
            {
                SubwayTag sprayed = _store.Spray(SubwayWall.OldTown, playerId, name, paint, out made);

                if (made)
                {
                    _store.Place(sprayed.Id, place);
                    sprayed.Place = place;
                }

                return sprayed;
            },
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
