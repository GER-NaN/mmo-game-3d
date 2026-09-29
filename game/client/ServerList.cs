namespace MmoGame3d.Client;

/// <summary>
/// The game's servers, by name, for the main menu's picker. Players meet only on the
/// same server. Servers are named after US cities; the names kept for servers to come
/// are Boston, Philadelphia, Pittsburgh and Minneapolis.
/// </summary>
public static class ServerList
{
    public static readonly GameServer[] All =
    {
        new GameServer("New York", "127.0.0.1"),
    };

    // The server so named, or the first for a name no longer on the list.
    public static GameServer Named(string name)
    {
        foreach (GameServer server in All)
        {
            if (server.Name == name)
            {
                return server;
            }
        }

        return All[0];
    }

    // The name of the server at this address, or the address itself for one not on the
    // list (--address).
    public static string NameFor(string address)
    {
        foreach (GameServer server in All)
        {
            if (server.Address == address)
            {
                return server.Name;
            }
        }

        return address;
    }
}
