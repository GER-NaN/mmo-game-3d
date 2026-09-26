namespace MmoGame3d.Networking;

using System;
using System.Diagnostics;
using Godot;

/// <summary>
/// The friends and ignore RPCs. A player is added by their peer (whoever is selected),
/// since names are not unique; removed by their player id, since they may be offline.
/// The server answers every change with the whole list, which is small.
/// </summary>
public partial class SocialNetwork : NetworkNode
{
    // Server side: (peer, target peer) and (peer, player id).
    public event Action<long, long>? BefriendRequested;
    public event Action<long, long>? IgnoreRequested;
    public event Action<long, string>? RemoveRequested;

    // Client side: friends (ids, names, zones; the zone is "" while offline), then the
    // ignored (ids, names).
    public event Action<string[], string[], string[], string[], string[]>? ContactsReceived;

    public void SendBefriend(long targetPeer)
    {
        RpcId(1, MethodName.Befriend, targetPeer);
    }

    public void SendIgnore(long targetPeer)
    {
        RpcId(1, MethodName.Ignore, targetPeer);
    }

    public void SendRemove(string playerId)
    {
        RpcId(1, MethodName.Remove, playerId);
    }

    public void SendContacts(long peer, string[] friendIds, string[] friendNames, string[] friendZones, string[] ignoredIds, string[] ignoredNames)
    {
        SendTo(peer, MethodName.ReceiveContacts, friendIds, friendNames, friendZones, ignoredIds, ignoredNames);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Befriend(long targetPeer)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.Befriend, sender, targetPeer))
            {
                BefriendRequested?.Invoke(sender, targetPeer);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Ignore(long targetPeer)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.Ignore, sender, targetPeer))
            {
                IgnoreRequested?.Invoke(sender, targetPeer);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Remove(string playerId)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.Remove, sender, playerId))
            {
                RemoveRequested?.Invoke(sender, playerId);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveContacts(string[] friendIds, string[] friendNames, string[] friendZones, string[] ignoredIds, string[] ignoredNames)
    {
        ContactsReceived?.Invoke(friendIds, friendNames, friendZones, ignoredIds, ignoredNames);
    }
}
