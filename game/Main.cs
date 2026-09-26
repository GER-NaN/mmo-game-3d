namespace MmoGame3d;

using System;
using Godot;

// Starts the game as a server or as a client. The same scene runs on both, so node
// paths match on each side, which RPCs and sync rely on.
public partial class Main : Node3D
{
    private const int Port = 7070;
    private const string ServerAddress = "127.0.0.1";

    // Where players stand on arrival: a row, so two joining at once do not overlap.
    private const float SpawnSpacing = 1.5f;
    private const int SpawnsPerRow = 5;

    private readonly PackedScene _playerScene = GD.Load<PackedScene>("res://game/player/Player.tscn");
    private Node3D _players = null!;
    private int _spawned;

    public override void _Ready()
    {
        _players = GetNode<Node3D>("Players");

        if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--server") >= 0)
        {
            StartServer();
        }
        else
        {
            StartClient();
        }
    }

    private void StartServer()
    {
        ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
        Error error = peer.CreateServer(Port);

        if (error != Error.Ok)
        {
            GD.PrintErr("Could not listen on port " + Port + ": " + error);
            GetTree().Quit(1);
            return;
        }

        Multiplayer.MultiplayerPeer = peer;
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
        GD.Print("Server listening on port " + Port);
    }

    private void StartClient()
    {
        ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
        Error error = peer.CreateClient(ServerAddress, Port);

        if (error != Error.Ok)
        {
            GD.PrintErr("Could not start the connection to " + ServerAddress + ":" + Port + ": " + error);
            return;
        }

        Multiplayer.MultiplayerPeer = peer;
        Multiplayer.ConnectedToServer += OnConnectedToServer;
        Multiplayer.ConnectionFailed += OnConnectionFailed;
        Multiplayer.ServerDisconnected += OnServerDisconnected;
    }

    private void OnConnectedToServer()
    {
        GD.Print("Connected as peer " + Multiplayer.GetUniqueId());
    }

    private void OnConnectionFailed()
    {
        GD.PrintErr("Could not connect to " + ServerAddress + ":" + Port);
    }

    private void OnServerDisconnected()
    {
        GD.PrintErr("The server closed the connection");
    }

    // Only the server adds players. The spawner then creates the same node on every
    // client, under the same name, so the paths match.
    private void OnPeerConnected(long id)
    {
        Player player = _playerScene.Instantiate<Player>();
        player.Name = id.ToString();
        player.Position = new Vector3((_spawned % SpawnsPerRow) * SpawnSpacing, 0f, (_spawned / SpawnsPerRow) * SpawnSpacing);
        _spawned++;
        _players.AddChild(player, true);
        GD.Print("Peer " + id + " joined");
    }

    private void OnPeerDisconnected(long id)
    {
        Node? player = _players.GetNodeOrNull(id.ToString());

        if (player != null)
        {
            player.QueueFree();
        }

        GD.Print("Peer " + id + " left");
    }
}
