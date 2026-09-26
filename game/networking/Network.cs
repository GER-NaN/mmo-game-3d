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

    // Client side: the whole bag, packed (see InventoryWire), sent to its owner only.
    public event Action<int[]>? InventoryReceived;

    // Client side: a short line for the player ("Picked up 2 GPU core").
    public event Action<string>? NoticeReceived;

    // Server side: (peer, text) a player typed.
    public event Action<long, string>? ChatRequested;

    // Client side: (sender, text, kind) - see ChatKind.
    public event Action<string, string, int>? ChatReceived;

    public void SendChat(string text)
    {
        RpcId(1, MethodName.Chat, text);
    }

    public void SendChatLine(long peer, string sender, string text, int kind)
    {
        RpcId(peer, MethodName.ReceiveChatLine, sender, text, kind);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Chat(string text)
    {
        if (Multiplayer.IsServer())
        {
            ChatRequested?.Invoke(Multiplayer.GetRemoteSenderId(), text);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveChatLine(string sender, string text, int kind)
    {
        ChatReceived?.Invoke(sender, text, kind);
    }

    public void SendInventory(long peer, int[] packed)
    {
        RpcId(peer, MethodName.ReceiveInventory, packed);
    }

    public void SendNotice(long peer, string text)
    {
        RpcId(peer, MethodName.ReceiveNotice, text);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveInventory(int[] packed)
    {
        InventoryReceived?.Invoke(packed);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveNotice(string text)
    {
        NoticeReceived?.Invoke(text);
    }

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
