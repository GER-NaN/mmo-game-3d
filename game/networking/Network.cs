namespace MmoGame3d.Networking;

using System;
using Godot;

/// <summary>
/// The session RPCs: login and entering the world. The node sits at the same path on
/// server and client, which is how an RPC finds its way. Each RPC only raises an event;
/// ServerGame and ClientGame decide what it means.
///
/// Who may call what: Login and WorldReady come from any peer and are handled only on
/// the server, which knows the sender from the connection. The answers are Authority
/// RPCs, so only the server can send them.
/// </summary>
public partial class Network : Node
{
    // Server side: (peer, protocol, license key, display name).
    public event Action<long, int, string, string>? LoginRequested;
    public event Action<long>? WorldReadyReceived;

    // Client side: (zone, display name), and the refusal's reason.
    public event Action<string, string>? LoginAccepted;
    public event Action<string>? LoginRefused;

    public void SendLogin(int protocol, string licenseKey, string displayName)
    {
        RpcId(1, MethodName.Login, protocol, licenseKey, displayName);
    }

    public void SendWorldReady()
    {
        RpcId(1, MethodName.WorldReady);
    }

    public void SendLoginAccepted(long peer, string zone, string displayName)
    {
        RpcId(peer, MethodName.Accept, zone, displayName);
    }

    public void SendLoginRefused(long peer, string reason)
    {
        RpcId(peer, MethodName.Refuse, reason);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Login(int protocol, string licenseKey, string displayName)
    {
        if (Multiplayer.IsServer())
        {
            LoginRequested?.Invoke(Multiplayer.GetRemoteSenderId(), protocol, licenseKey, displayName);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void WorldReady()
    {
        if (Multiplayer.IsServer())
        {
            WorldReadyReceived?.Invoke(Multiplayer.GetRemoteSenderId());
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Accept(string zone, string displayName)
    {
        LoginAccepted?.Invoke(zone, displayName);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Refuse(string reason)
    {
        LoginRefused?.Invoke(reason);
    }
}
