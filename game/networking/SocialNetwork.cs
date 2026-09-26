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

    // Server side, Whois: search text, a page by player id, props on a page, the owner's
    // own settings, and befriending from a page (the player may be offline).
    public event Action<long, string>? WhoisSearchRequested;
    public event Action<long, string>? WhoisOpenRequested;
    public event Action<long, string>? WhoisPropsRequested;
    public event Action<long, string, bool, bool>? WhoisEditRequested;
    public event Action<long, string>? BefriendIdRequested;

    // Client side, Whois: search results (ids, names, career titles, levels, 1 if online)
    // and one page (see ServerWhois for its keys).
    public event Action<string[], string[], string[], int[], int[]>? WhoisResultsReceived;
    public event Action<Godot.Collections.Dictionary>? WhoisPageReceived;

    public void SendWhoisSearch(string text)
    {
        RpcId(1, MethodName.WhoisSearch, text);
    }

    public void SendWhoisOpen(string playerId)
    {
        RpcId(1, MethodName.WhoisOpen, playerId);
    }

    public void SendWhoisProps(string playerId)
    {
        RpcId(1, MethodName.WhoisProps, playerId);
    }

    public void SendWhoisEdit(string plan, bool showSkills, bool showLocation)
    {
        RpcId(1, MethodName.WhoisEdit, plan, showSkills, showLocation);
    }

    public void SendBefriendId(string playerId)
    {
        RpcId(1, MethodName.BefriendId, playerId);
    }

    public void SendWhoisResults(long peer, string[] ids, string[] names, string[] titles, int[] levels, int[] online)
    {
        SendTo(peer, MethodName.ReceiveWhoisResults, ids, names, titles, levels, online);
    }

    public void SendWhoisPage(long peer, Godot.Collections.Dictionary page)
    {
        SendTo(peer, MethodName.ReceiveWhoisPage, page);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveWhoisResults(string[] ids, string[] names, string[] titles, int[] levels, int[] online)
    {
        WhoisResultsReceived?.Invoke(ids, names, titles, levels, online);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveWhoisPage(Godot.Collections.Dictionary page)
    {
        WhoisPageReceived?.Invoke(page);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void WhoisSearch(string text)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.WhoisSearch, sender, text))
            {
                WhoisSearchRequested?.Invoke(sender, text);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void WhoisOpen(string playerId)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.WhoisOpen, sender, playerId))
            {
                WhoisOpenRequested?.Invoke(sender, playerId);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void WhoisProps(string playerId)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.WhoisProps, sender, playerId))
            {
                WhoisPropsRequested?.Invoke(sender, playerId);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void WhoisEdit(string plan, bool showSkills, bool showLocation)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.WhoisEdit, sender, plan, showSkills, showLocation))
            {
                WhoisEditRequested?.Invoke(sender, plan, showSkills, showLocation);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void BefriendId(string playerId)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.BefriendId, sender, playerId))
            {
                BefriendIdRequested?.Invoke(sender, playerId);
            }
        }
    }

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
