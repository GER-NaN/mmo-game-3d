namespace MmoGame3d.Server;

using Godot;

/// <summary>
/// The server's ENet peer, wrapped so every packet in and out is logged: RPCs, and the
/// replication our code never sees (synced positions, spawns and despawns). The
/// multiplayer API talks to this peer and it passes everything to the ENet one.
/// </summary>
public partial class LoggedPeer : MultiplayerPeerExtension
{
    // What ENet's multiplayer peer allows.
    private const int MaxPacketSize = 1 << 24;

    private ENetMultiplayerPeer _inner = null!;
    private ServerDiagnostics _diagnostics = null!;
    private int _targetPeer;
    private int _channel;
    private TransferModeEnum _mode = TransferModeEnum.Reliable;

    public void Wrap(ENetMultiplayerPeer inner, ServerDiagnostics diagnostics)
    {
        _inner = inner;
        _diagnostics = diagnostics;
        _inner.PeerConnected += id => EmitSignal(MultiplayerPeer.SignalName.PeerConnected, id);
        _inner.PeerDisconnected += id => EmitSignal(MultiplayerPeer.SignalName.PeerDisconnected, id);
    }

    public override int _GetAvailablePacketCount()
    {
        return _inner.GetAvailablePacketCount();
    }

    // The peer, channel and mode belong to the packet at the front of the queue, so they
    // are read before the packet is taken off it.
    public override byte[] _GetPacketScript()
    {
        int peer = _inner.GetPacketPeer();
        int channel = _inner.GetPacketChannel();
        TransferModeEnum mode = _inner.GetPacketMode();
        byte[] packet = _inner.GetPacket();
        _diagnostics.Packet(true, peer, channel, mode, packet);
        return packet;
    }

    public override Error _PutPacketScript(byte[] buffer)
    {
        Error error = _inner.PutPacket(buffer);
        _diagnostics.Packet(false, _targetPeer, _channel, _mode, buffer);
        return error;
    }

    public override int _GetPacketPeer()
    {
        return _inner.GetPacketPeer();
    }

    public override int _GetPacketChannel()
    {
        return _inner.GetPacketChannel();
    }

    public override TransferModeEnum _GetPacketMode()
    {
        return _inner.GetPacketMode();
    }

    public override int _GetMaxPacketSize()
    {
        return MaxPacketSize;
    }

    public override void _SetTransferChannel(int channel)
    {
        _channel = channel;
        _inner.TransferChannel = channel;
    }

    public override int _GetTransferChannel()
    {
        return _channel;
    }

    public override void _SetTransferMode(TransferModeEnum mode)
    {
        _mode = mode;
        _inner.TransferMode = mode;
    }

    public override TransferModeEnum _GetTransferMode()
    {
        return _mode;
    }

    public override void _SetTargetPeer(int peer)
    {
        _targetPeer = peer;
        _inner.SetTargetPeer(peer);
    }

    public override bool _IsServer()
    {
        return _inner.GetUniqueId() == 1;
    }

    public override void _Poll()
    {
        _inner.Poll();
    }

    public override void _Close()
    {
        _inner.Close();
    }

    public override void _DisconnectPeer(int peer, bool force)
    {
        _inner.DisconnectPeer(peer, force);
    }

    public override int _GetUniqueId()
    {
        return _inner.GetUniqueId();
    }

    public override void _SetRefuseNewConnections(bool enable)
    {
        _inner.RefuseNewConnections = enable;
    }

    public override bool _IsRefusingNewConnections()
    {
        return _inner.RefuseNewConnections;
    }

    public override bool _IsServerRelaySupported()
    {
        return _inner.IsServerRelaySupported();
    }

    public override ConnectionStatus _GetConnectionStatus()
    {
        return _inner.GetConnectionStatus();
    }
}
