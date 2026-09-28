namespace MmoGame3d;

using Godot;
using MmoGame3d.Client;
using MmoGame3d.Dev;
using MmoGame3d.Networking;
using MmoGame3d.Server;
using MmoGame3d.Zones;

/// <summary>
/// Starts the game as the server or as a client, from the command line. Both sides
/// build the same tree under Main: the RPC nodes (see Networks) and "World" for the
/// zones, because RPCs and sync find their nodes by path.
/// </summary>
public partial class Main : Node
{
    private static readonly PackedScene WorldScene = GD.Load<PackedScene>("res://game/zones/World.tscn");

    // Set by the load test on each bot's Main before it enters the tree; otherwise the
    // options come from the command line.
    public LaunchOptions? Options { get; set; }

    public override void _Ready()
    {
        LaunchOptions options = Options ?? LaunchOptions.Parse(OS.GetCmdlineUserArgs());

        if (options.CheckScenes)
        {
            GetTree().Quit(SceneCheck.Run("res://game") == 0 ? 0 : 1);
            return;
        }

        if (options.LoadTestBots > 0 && Options == null)
        {
            LoadTest host = new LoadTest { Name = "LoadTest" };
            AddChild(host);
            host.Start(options);
            return;
        }

        Networks networks = new Networks(this);

        if (options.IsServer)
        {
            World world = WorldScene.Instantiate<World>();
            world.Name = "World";
            world.OwnPhysicsPerZone = true;
            AddChild(world);

            ServerGame server = new ServerGame { Name = "ServerGame" };
            AddChild(server);
            server.Start(options, networks, world);
        }
        else
        {
            ClientGame client = new ClientGame { Name = "ClientGame" };
            AddChild(client);
            client.Start(options, networks, this);
        }
    }
}
