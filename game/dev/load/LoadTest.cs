namespace MmoGame3d.Dev;

using Godot;

/// <summary>
/// Runs many bot clients in one process, for load tests. Each bot is a whole client:
/// its own Main under its own node, with a multiplayer of its own for that branch of
/// the tree, so it has its own connection and peer id. A branch's node paths are taken
/// relative to its root, so each bot's "Main/Network" meets the server's.
///
/// Bots join a few a second rather than all at once, like players arriving.
/// </summary>
public partial class LoadTest : Node
{
    private const double JoinInterval = 0.2;
    private const double ReportInterval = 10;

    private static readonly PackedScene MainScene = GD.Load<PackedScene>("res://game/Main.tscn");

    private LaunchOptions _options = null!;
    private int _started;
    private double _sinceJoin;
    private double _sinceReport;

    public void Start(LaunchOptions options)
    {
        _options = options;

        // Headless windows are 64 by 64; nothing here draws, but keep it sane.
        GetTree().Root.Size = new Vector2I(1280, 720);
        Players.Player.DrawModels = false;
        GD.Print("Load test: starting " + options.LoadTestBots + " bots, from load-" + options.LoadFirst);
    }

    public override void _Process(double delta)
    {
        _sinceJoin += delta;

        if (_started < _options.LoadTestBots && _sinceJoin >= JoinInterval)
        {
            _sinceJoin = 0;
            StartBot(_options.LoadFirst + _started);
            _started++;
        }

        _sinceReport += delta;

        if (_sinceReport >= ReportInterval)
        {
            _sinceReport = 0;
            Report();
        }
    }

    private void StartBot(int number)
    {
        Node branch = new Node { Name = "Bot" + number };
        AddChild(branch);
        GetTree().SetMultiplayer(MultiplayerApi.CreateDefaultInterface(), branch.GetPath());

        Main main = MainScene.Instantiate<Main>();
        main.Options = _options.ForLoadBot(number);
        branch.AddChild(main);
    }

    private void Report()
    {
        int connected = 0;

        foreach (Node branch in GetChildren())
        {
            MultiplayerApi api = GetTree().GetMultiplayer(branch.GetPath());

            if (api.HasMultiplayerPeer() && api.MultiplayerPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected)
            {
                connected++;
            }
        }

        GD.Print(string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "Load test: {0} of {1} bots connected, {2} fps, frame {3:F1} ms, physics {4:F1} ms, {5} nodes, {6} physics objects",
            connected,
            _started,
            Engine.GetFramesPerSecond(),
            Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000.0,
            Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000.0,
            Performance.GetMonitor(Performance.Monitor.ObjectNodeCount),
            Performance.GetMonitor(Performance.Monitor.Physics3DActiveObjects)));
    }
}
