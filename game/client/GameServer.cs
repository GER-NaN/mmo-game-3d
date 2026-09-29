namespace MmoGame3d.Client;

// One of the game's servers: its name on the main menu, and where it is.
public class GameServer
{
    public GameServer(string name, string address)
    {
        Name = name;
        Address = address;
    }

    public string Name { get; }

    public string Address { get; }
}
