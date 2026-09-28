namespace MmoGame3d.Client;

using MmoGame3d.Rules.Items;

/// <summary>
/// What this client knows of its own player, read-only: where it is, its money, what it
/// carries and wears. For code that reads the client's state rather than its screens.
/// A snapshot: taken fresh from ClientGame.View each time it is wanted.
/// </summary>
public class ClientView
{
    public ClientView(string zoneId, int dollars, Belongings belongings)
    {
        ZoneId = zoneId;
        Dollars = dollars;
        Belongings = belongings;
    }

    // Empty before the player is in the world.
    public string ZoneId { get; }

    public int Dollars { get; }

    public Belongings Belongings { get; }
}
