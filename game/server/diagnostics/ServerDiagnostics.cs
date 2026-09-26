namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using Microsoft.Extensions.Logging;
using MmoGame3d.Diagnostics;
using MmoGame3d.Networking;
using MmoGame3d.Rules;

/// <summary>
/// The server's logs and traces: every packet in and out (LoggedPeer), every RPC
/// received (a span) and sent (a record), everything Godot prints (EngineLog), and the
/// spans the game opens itself (Source). Records about a peer carry that peer's context
/// (who they are, where they are), set by the game with Tag, so a viewer can group the
/// traffic by player.
///
/// Called on the game thread, except EngineLog. Writing a record only queues it; see
/// Telemetry.
/// </summary>
public sealed class ServerDiagnostics : IRpcLog, IDisposable
{
    public const string Service = "mmo-server";

    // The game's own spans: travel, saves, and whatever is worth timing next.
    public static readonly ActivitySource Source = new ActivitySource("MmoGame3d.Server");

    private const double HealthIntervalSeconds = 10;

    // The first byte of a packet from Godot's scene multiplayer says what it carries: the
    // low three bits are the command, in the order of SceneMultiplayer's NetworkCommands.
    // Read from the engine source, not a public API, so treat the names as a good guess.
    private static readonly string[] PacketKinds =
    {
        "remote_call", "simplify_path", "confirm_path", "raw", "spawn", "despawn", "sync", "sys",
    };

    private static readonly KeyValuePair<string, object?>[] NoContext = Array.Empty<KeyValuePair<string, object?>>();

    private readonly Telemetry _telemetry;
    private readonly ILogger _packets;
    private readonly ILogger _rpcs;
    private readonly ILogger _health;
    private readonly EngineLog _engineLog;

    // Replaced, never changed, so a record already queued keeps the context it had.
    private readonly Dictionary<long, KeyValuePair<string, object?>[]> _peers = new Dictionary<long, KeyValuePair<string, object?>[]>();

    private double _sinceHealth;
    private long _packetsIn;
    private long _packetsOut;
    private long _bytesIn;
    private long _bytesOut;

    public ServerDiagnostics(string filePath)
    {
        _telemetry = new Telemetry(Service, GameVersion.Protocol.ToString(), filePath);
        _packets = _telemetry.Logger("Net.Packets");
        _rpcs = _telemetry.Logger("Net.Rpc");
        _health = _telemetry.Logger("Diagnostics");
        _engineLog = new EngineLog { Target = _telemetry.Logger("Engine") };
        OS.AddLogger(_engineLog);
    }

    public string FilePath
    {
        get { return _telemetry.FilePath; }
    }

    // Adds to or changes a peer's context: player.name, player.zone and so on.
    public void Tag(long peer, string key, object? value)
    {
        KeyValuePair<string, object?>[]? old;
        _peers.TryGetValue(peer, out old);
        List<KeyValuePair<string, object?>> context = new List<KeyValuePair<string, object?>>();

        if (old != null)
        {
            foreach (KeyValuePair<string, object?> item in old)
            {
                if (item.Key != key)
                {
                    context.Add(item);
                }
            }
        }

        context.Add(new KeyValuePair<string, object?>(key, value));
        _peers[peer] = context.ToArray();
    }

    public void Forget(long peer)
    {
        _peers.Remove(peer);
    }

    public void Packet(bool incoming, long peer, int channel, MultiplayerPeer.TransferModeEnum mode, byte[] data)
    {
        if (incoming)
        {
            _packetsIn++;
            _bytesIn += data.Length;
        }
        else
        {
            _packetsOut++;
            _bytesOut += data.Length;
        }

        Fields fields = new Fields(incoming ? "packet in" : "packet out", 7)
            .With("net.direction", incoming ? "in" : "out")
            .With("net.peer", peer)
            .With("net.channel", channel)
            .With("net.mode", ModeName(mode))
            .With("net.kind", data.Length > 0 ? PacketKinds[data[0] & 7] : "empty")
            .With("net.bytes", data.Length)
            .With("net.payload", data)
            .WithAll(ContextOf(peer));
        _packets.Write(LogLevel.Debug, fields);
    }

    public Activity? Received(string node, StringName method, long peer, object?[] args)
    {
        string methodName = method.ToString();
        Activity? span = Source.StartActivity(node + "/" + methodName, ActivityKind.Server);

        if (span == null)
        {
            return null;
        }

        span.SetTag("rpc.system", "godot");
        span.SetTag("rpc.service", node);
        span.SetTag("rpc.method", methodName);
        span.SetTag("net.peer", peer);
        span.SetTag("rpc.args", args);

        foreach (KeyValuePair<string, object?> item in ContextOf(peer))
        {
            span.SetTag(item.Key, item.Value);
        }

        return span;
    }

    // Variants are read here, on the game thread; the exporter thread gets plain values.
    public void Sent(string node, StringName method, long peer, Variant[] args)
    {
        object?[] values = new object?[args.Length];

        for (int i = 0; i < args.Length; i++)
        {
            values[i] = args[i].Obj;
        }

        Fields fields = new Fields("rpc out", 5)
            .With("rpc.service", node)
            .With("rpc.method", method.ToString())
            .With("net.peer", peer)
            .With("rpc.args", values)
            .WithAll(ContextOf(peer));
        _rpcs.Write(LogLevel.Debug, fields);
    }

    // A record every few seconds on the traffic and on what the logging dropped, so a
    // gap in the log is visible as a gap rather than silence.
    public void Tick(double delta)
    {
        _sinceHealth += delta;

        if (_sinceHealth < HealthIntervalSeconds)
        {
            return;
        }

        Fields fields = new Fields("diagnostics", 7)
            .With("interval_s", _sinceHealth)
            .With("net.packets_in", _packetsIn)
            .With("net.packets_out", _packetsOut)
            .With("net.bytes_in", _bytesIn)
            .With("net.bytes_out", _bytesOut)
            .With("logs.dropped_total", _telemetry.LogsDropped)
            .With("spans.dropped_total", _telemetry.SpansDropped);
        _health.Write(LogLevel.Information, fields);

        _sinceHealth = 0;
        _packetsIn = 0;
        _packetsOut = 0;
        _bytesIn = 0;
        _bytesOut = 0;
    }

    // The engine logger goes first, so nothing the shutdown prints reaches a closed log.
    public void Dispose()
    {
        OS.RemoveLogger(_engineLog);
        _engineLog.Target = null;
        _telemetry.Dispose();
    }

    private KeyValuePair<string, object?>[] ContextOf(long peer)
    {
        KeyValuePair<string, object?>[]? context;
        return _peers.TryGetValue(peer, out context) ? context : NoContext;
    }

    private static string ModeName(MultiplayerPeer.TransferModeEnum mode)
    {
        switch (mode)
        {
            case MultiplayerPeer.TransferModeEnum.Reliable:
                return "reliable";
            case MultiplayerPeer.TransferModeEnum.UnreliableOrdered:
                return "unreliable_ordered";
            default:
                return "unreliable";
        }
    }
}
