namespace MmoGame3d.Server;

using System;
using Godot;

/// <summary>
/// Every packet in and out, replication included, through the native PacketLogPeer
/// (native/src/packet_log_peer.cpp). The peer records packets in C++; this takes the
/// records once a frame and hands them to ServerDiagnostics, so the engine never calls
/// into C# per packet or per synchronizer. Only with --log-packets.
/// </summary>
public sealed class NativePacketLog
{
    private const string ExtensionPath = "res://native/packet_log.gdextension";
    private const int RecordHeaderSize = 1 + 4 + 1 + 1 + 4;

    private readonly GodotObject _peer;
    private readonly ServerDiagnostics _diagnostics;

    private NativePacketLog(GodotObject peer, ServerDiagnostics diagnostics)
    {
        _peer = peer;
        _diagnostics = diagnostics;
    }

    // The engine talks to this peer; it passes everything to the ENet one.
    public MultiplayerPeer Peer
    {
        get { return (MultiplayerPeer)_peer; }
    }

    // Null, with the reason printed, when the extension is not built.
    public static NativePacketLog? Wrap(ENetMultiplayerPeer inner, ServerDiagnostics diagnostics)
    {
        GDExtensionManager.LoadStatus status = GDExtensionManager.LoadExtension(ExtensionPath);

        if (status != GDExtensionManager.LoadStatus.Ok && status != GDExtensionManager.LoadStatus.AlreadyLoaded)
        {
            GD.PrintErr("The packet log needs the native build (scripts/native-build.ps1); running without it (" + status + ")");
            return null;
        }

        GodotObject peer = ClassDB.Instantiate("PacketLogPeer").AsGodotObject();
        peer.Call("wrap", inner);
        return new NativePacketLog(peer, diagnostics);
    }

    // Once a frame: every packet since the last call, one crossing into C# for them all.
    public void Drain()
    {
        byte[] records = _peer.Call("drain").AsByteArray();
        int at = 0;

        while (at + RecordHeaderSize <= records.Length)
        {
            bool incoming = records[at] == 0;
            int peer = BitConverter.ToInt32(records, at + 1);
            int channel = records[at + 5];
            MultiplayerPeer.TransferModeEnum mode = (MultiplayerPeer.TransferModeEnum)records[at + 6];
            int size = BitConverter.ToInt32(records, at + 7);
            at += RecordHeaderSize;

            byte[] payload = new byte[size];
            Buffer.BlockCopy(records, at, payload, 0, size);
            at += size;

            _diagnostics.Packet(incoming, peer, channel, mode, payload);
        }

        long dropped = _peer.Call("take_dropped").AsInt64();

        if (dropped > 0)
        {
            _diagnostics.PacketsDropped(dropped);
        }
    }
}
