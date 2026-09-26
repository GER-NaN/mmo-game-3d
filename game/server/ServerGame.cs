namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Data;
using MmoGame3d.Data.Accounts;
using MmoGame3d.Data.Players;
using MmoGame3d.Networking;
using MmoGame3d.Players;
using MmoGame3d.Rules;
using MmoGame3d.Rules.Players;
using MmoGame3d.Rules.World;
using MmoGame3d.Zones;

/// <summary>
/// The server's side of the game: who is connected, logging in and out, putting bodies
/// in the world, and writing players down. Game state is only touched on this thread;
/// the database runs on the persistence worker and answers through its completions.
/// </summary>
public partial class ServerGame : Node
{
    // How often players online are saved. Ending a session saves too, so this only
    // bounds what a server crash can lose.
    private const double SaveIntervalSeconds = 30;

    private static readonly TimeSpan ShutdownDrain = TimeSpan.FromSeconds(10);
    private static readonly PackedScene PlayerScene = GD.Load<PackedScene>("res://game/player/Player.tscn");

    private readonly Dictionary<long, Session> _sessions = new Dictionary<long, Session>();

    private LaunchOptions _options = null!;
    private Network _network = null!;
    private World _world = null!;
    private PersistenceWorker _worker = null!;
    private AccountStore _accounts = null!;
    private PlayerStore _players = null!;
    private StopSignals? _stopSignals;
    private double _sinceSave;

    public void Start(LaunchOptions options, Network network, World world)
    {
        _options = options;
        _network = network;
        _world = world;

        foreach (string zoneId in ZoneIds.All)
        {
            _world.LoadZone(zoneId);
        }

        // Migrations run before the server listens: a schema that cannot be brought up
        // to date is a server that must not take players.
        Database database = new Database(options.DatabaseConnection);

        try
        {
            IReadOnlyList<string> applied = MigrationRunner.Run(database);
            GD.Print("Database ready; migrations applied: " + (applied.Count == 0 ? "none" : string.Join(", ", applied)));
        }
        catch (Exception e)
        {
            GD.PrintErr("Could not prepare the database: " + e.Message);
            GetTree().Quit(1);
            return;
        }

        _accounts = new AccountStore(database);
        _players = new PlayerStore(database);
        _worker = new PersistenceWorker();
        _stopSignals = new StopSignals(options.Port);

        ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
        Error error = peer.CreateServer(options.Port, options.MaxPlayers);

        if (error != Error.Ok)
        {
            GD.PrintErr("Could not listen on port " + options.Port + ": " + error);
            GetTree().Quit(1);
            return;
        }

        Multiplayer.MultiplayerPeer = peer;
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
        _network.LoginRequested += OnLoginRequested;
        _network.WorldReadyReceived += OnWorldReady;

        GD.Print("Server listening on port " + options.Port + " for up to " + options.MaxPlayers + " players");
    }

    public override void _Process(double delta)
    {
        if (_worker == null)
        {
            return;
        }

        _worker.RunCompletions();

        if (_stopSignals != null && _stopSignals.StopRequested())
        {
            GD.Print("Stopping: saving players online");
            GetTree().Quit();
            return;
        }

        _sinceSave += delta;

        if (_sinceSave >= SaveIntervalSeconds)
        {
            _sinceSave = 0;
            SaveEveryone();
        }
    }

    public override void _ExitTree()
    {
        if (_worker == null)
        {
            return;
        }

        SaveEveryone();

        if (!_worker.Stop(ShutdownDrain))
        {
            GD.PrintErr("Shutdown: the last saves did not finish in time");
        }

        _worker.Dispose();

        if (_stopSignals != null)
        {
            _stopSignals.MarkStopped();
            _stopSignals.Dispose();
        }

        GD.Print("Server stopped");
    }

    private void OnPeerConnected(long peer)
    {
        _sessions[peer] = new Session(peer);
        GD.Print("Peer " + peer + " connected");
    }

    private void OnPeerDisconnected(long peer)
    {
        if (!_sessions.TryGetValue(peer, out Session? session))
        {
            return;
        }

        _sessions.Remove(peer);

        if (session.Body != null)
        {
            Save(session);
            session.Body.QueueFree();
        }

        GD.Print("Peer " + peer + " left" + (session.Record != null ? " (" + session.Record.DisplayName + ")" : ""));
    }

    private void OnLoginRequested(long peer, int protocol, string licenseKeyText, string displayName)
    {
        if (!_sessions.TryGetValue(peer, out Session? session) || session.State != SessionState.Connected)
        {
            return;
        }

        if (protocol != GameVersion.Protocol)
        {
            Refuse(peer, "This game is version " + protocol + " and the server is version " + GameVersion.Protocol + ". Update the game.");
            return;
        }

        Guid licenseKey;

        if (!Guid.TryParse(licenseKeyText, out licenseKey))
        {
            Refuse(peer, "The license key is not valid.");
            return;
        }

        string? nameProblem = DisplayName.Problem(displayName);

        if (nameProblem != null)
        {
            Refuse(peer, nameProblem);
            return;
        }

        session.State = SessionState.LoggingIn;

        Zone start = _world.GetZone(ZoneIds.Start)!;
        Vector3 spawn = start.SpawnPoint;
        string name = displayName.Trim();

        _worker.Enqueue(
            () =>
            {
                Guid accountId = _accounts.GetOrCreate(licenseKey);
                PlayerRecord newPlayer = new PlayerRecord
                {
                    PlayerId = Guid.NewGuid(),
                    AccountId = accountId,
                    DisplayName = name,
                    Zone = ZoneIds.Start,
                    PositionX = spawn.X,
                    PositionY = spawn.Y,
                    PositionZ = spawn.Z,
                };
                return _players.GetOrCreate(newPlayer, out _);
            },
            record => OnPlayerLoaded(peer, record),
            e =>
            {
                GD.PrintErr("Login for peer " + peer + " failed: " + e.Message);
                Refuse(peer, "The server could not load your player. Try again.");
            });
    }

    private void OnPlayerLoaded(long peer, PlayerRecord record)
    {
        // The client may have gone while the lookup ran.
        if (!_sessions.TryGetValue(peer, out Session? session))
        {
            return;
        }

        foreach (Session other in _sessions.Values)
        {
            if (other != session && other.Record != null && other.Record.AccountId == record.AccountId)
            {
                Refuse(peer, "This player is already online.");
                return;
            }
        }

        // A zone removed since the player was saved would lock them out; they start over.
        if (_world.GetZone(record.Zone) == null)
        {
            Zone start = _world.GetZone(ZoneIds.Start)!;
            record.Zone = ZoneIds.Start;
            record.PositionX = start.SpawnPoint.X;
            record.PositionY = start.SpawnPoint.Y;
            record.PositionZ = start.SpawnPoint.Z;
        }

        session.Record = record;
        session.State = SessionState.Accepted;
        _network.SendLoginAccepted(peer, record.Zone, record.DisplayName);
        GD.Print("Peer " + peer + " logged in as " + record.DisplayName + " (player " + record.PlayerId + ")");
    }

    // The client has its zone loaded, so the body can be spawned there without the
    // spawn arriving before the node it goes under.
    private void OnWorldReady(long peer)
    {
        if (!_sessions.TryGetValue(peer, out Session? session) || session.State != SessionState.Accepted || session.Record == null)
        {
            return;
        }

        PlayerRecord record = session.Record;
        Zone zone = _world.GetZone(record.Zone)!;

        Player body = PlayerScene.Instantiate<Player>();
        body.Name = peer.ToString();
        body.DisplayName = record.DisplayName;
        body.Position = FreeSpotNear(zone, new Vector3(record.PositionX, record.PositionY, record.PositionZ));
        body.Rotation = new Vector3(0f, record.Yaw, 0f);
        body.RespawnPoint = zone.SpawnPoint;

        // A client is sent this body only while it is in the world and in the same zone.
        // Godot shows a synchronizer to a peer when it is public and every filter agrees,
        // so the filter is the whole gate. Visibility also decides spawning: the body is
        // created on a client when it becomes visible there, and removed when it stops.
        body.Synchronizer.AddVisibilityFilter(Callable.From<long, bool>(viewer => CanSee(viewer, zone.ZoneId)));

        zone.Players.AddChild(body, true);
        session.Body = body;
        session.State = SessionState.InWorld;
    }

    // Two bodies placed in one spot push each other apart, hard enough to throw one into
    // the sky. So a body goes where the saved spot is, or the nearest free spot on rings
    // around it. The test body is lifted a little so the floor it stands on does not count.
    private static Vector3 FreeSpotNear(Zone zone, Vector3 wanted)
    {
        const float RingStep = 1.5f;
        const int Rings = 4;
        const int SpotsPerRing = 8;

        PhysicsDirectSpaceState3D space = zone.GetWorld3D().DirectSpaceState;
        PhysicsShapeQueryParameters3D query = new PhysicsShapeQueryParameters3D
        {
            Shape = new CapsuleShape3D(),
        };

        for (int ring = 0; ring <= Rings; ring++)
        {
            int spots = ring == 0 ? 1 : SpotsPerRing;

            for (int spot = 0; spot < spots; spot++)
            {
                float angle = Mathf.Tau * spot / spots;
                Vector3 candidate = wanted + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (ring * RingStep);
                query.Transform = new Transform3D(Basis.Identity, candidate + new Vector3(0f, 1.05f, 0f));

                if (space.IntersectShape(query, 1).Count == 0)
                {
                    return candidate;
                }
            }
        }

        return wanted;
    }

    private bool CanSee(long viewer, string zoneId)
    {
        return _sessions.TryGetValue(viewer, out Session? session)
            && session.State == SessionState.InWorld
            && session.ZoneId == zoneId;
    }

    private void Refuse(long peer, string reason)
    {
        _network.SendLoginRefused(peer, reason);
        GD.Print("Peer " + peer + " refused: " + reason);

        if (_sessions.TryGetValue(peer, out Session? session))
        {
            session.State = SessionState.Connected;
        }
    }

    private void SaveEveryone()
    {
        foreach (Session session in _sessions.Values)
        {
            if (session.Body != null)
            {
                Save(session);
            }
        }
    }

    // Copies the body's state into a fresh record for the worker, so the worker never
    // reads an object the game thread is still changing.
    private void Save(Session session)
    {
        PlayerRecord live = session.Record!;
        Player body = session.Body!;

        PlayerRecord snapshot = new PlayerRecord
        {
            PlayerId = live.PlayerId,
            AccountId = live.AccountId,
            DisplayName = live.DisplayName,
            Zone = live.Zone,
            PositionX = body.Position.X,
            PositionY = body.Position.Y,
            PositionZ = body.Position.Z,
            Yaw = body.Rotation.Y,
        };

        _worker.Enqueue(() => _players.Save(snapshot), e => GD.PrintErr("Saving " + snapshot.DisplayName + " failed: " + e.Message));
    }
}
