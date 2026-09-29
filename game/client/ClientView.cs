namespace MmoGame3d.Client;

using MmoGame3d.Rules.Items;

/// <summary>
/// What this client knows of its own player, read-only: where it is, its money, what it
/// carries and wears, what of this zone its map has discovered. For code that reads the
/// client's state rather than its screens.
/// A snapshot: taken fresh from ClientGame.View each time it is wanted.
/// </summary>
public class ClientView
{
    public ClientView(string zoneId, int dollars, Belongings belongings, byte[] mapCells)
    {
        ZoneId = zoneId;
        Dollars = dollars;
        Belongings = belongings;
        MapCells = mapCells;
    }

    // Empty before the player is in the world.
    public string ZoneId { get; }

    public int Dollars { get; }

    public Belongings Belongings { get; }

    // This zone's discovered map cells, as the server sent them (Discovery.ToBytes);
    // empty before any came.
    public byte[] MapCells { get; }
}
