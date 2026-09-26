namespace MmoGame3d;

using Godot;
using MmoGame3d.Client;
using MmoGame3d.Networking;
using MmoGame3d.Server;
using MmoGame3d.Zones;

/// <summary>
/// Starts the game as the server or as a client, from the command line. Both sides
/// build the same tree under Main: "Network" and "PartyNetwork" for the RPCs and "World"
/// for the zones, because RPCs and sync find their nodes by path.
/// </summary>
public partial class Main : Node
{
    private static readonly PackedScene WorldScene = GD.Load<PackedScene>("res://game/zones/World.tscn");

    public override void _Ready()
    {
        LaunchOptions options = LaunchOptions.Parse(OS.GetCmdlineUserArgs());
        Network network = GetNode<Network>("Network");
        PartyNetwork partyNetwork = GetNode<PartyNetwork>("PartyNetwork");

        if (options.IsServer)
        {
            World world = WorldScene.Instantiate<World>();
            world.Name = "World";
            AddChild(world);

            ServerGame server = new ServerGame { Name = "ServerGame" };
            AddChild(server);
            server.Start(options, network, partyNetwork, world);
        }
        else
        {
            ClientGame client = new ClientGame { Name = "ClientGame" };
            AddChild(client);
            client.Start(options, network, partyNetwork, this);
        }
    }
}
