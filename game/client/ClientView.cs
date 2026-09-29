namespace MmoGame3d.Client;

using System.Collections.Generic;
using MmoGame3d.Rules.Items;

/// <summary>
/// What this client knows of its own player, read-only: where it is, its money, what it
/// carries and wears, what of this zone its map has discovered, which screens it has open,
/// what it was last told.
/// For code that reads the client's state rather than its screens.
/// A snapshot: taken fresh from ClientGame.View each time it is wanted.
/// </summary>
public class ClientView
{
    public ClientView(string zoneId, int dollars, Belongings belongings, byte[] mapCells, IReadOnlyList<string> openScreens, IReadOnlyList<string> notices, int noticeCount)
    {
        Notices = notices;
        NoticeCount = noticeCount;
        ZoneId = zoneId;
        Dollars = dollars;
        Belongings = belongings;
        MapCells = mapCells;
        OpenScreens = openScreens;
    }

    // Empty before the player is in the world.
    public string ZoneId { get; }

    public int Dollars { get; }

    public Belongings Belongings { get; }

    // This zone's discovered map cells, as the server sent them (Discovery.ToBytes);
    // empty before any came.
    public byte[] MapCells { get; }

    // The screens open now, by name ("inventory", "terminal", "game-menu"), in no
    // particular order.
    public IReadOnlyList<string> OpenScreens { get; }

    // The newest notices the server sent ("You fixed the bench."), oldest first, and how
    // many have come since the client started: a reader keeps the count to tell the new.
    public IReadOnlyList<string> Notices { get; }

    public int NoticeCount { get; }
}
