namespace MmoGame3d.Networking;

using System;
using System.Diagnostics;
using Godot;

/// <summary>
/// The party RPCs. Like Network, it sits at the same path on both sides and only raises
/// events. Requests come from any peer and are handled only on the server, which knows
/// the sender from the connection; the answers are Authority RPCs.
///
/// Players are named on the wire by their player id, which is public. A body's node
/// name is its peer id, so an invite names its target that way.
/// </summary>
public partial class PartyNetwork : NetworkNode
{
    // Server side.
    public event Action<long, long>? InviteRequested;
    public event Action<long, string, bool>? ResponseReceived;
    public event Action<long>? LeaveRequested;
    public event Action<long, string>? ChatRequested;

    // Client side: (inviter id, inviter name).
    public event Action<string, string>? InviteReceived;

    // Client side: (leader id, member ids, names, online flags). No members: no party.
    public event Action<string, string[], string[], int[]>? PartyReceived;

    public void SendInvite(long targetPeer)
    {
        RpcId(1, MethodName.Invite, targetPeer);
    }

    public void SendResponse(string inviterId, bool accept)
    {
        RpcId(1, MethodName.Respond, inviterId, accept);
    }

    public void SendLeave()
    {
        RpcId(1, MethodName.Leave);
    }

    public void SendChat(string text)
    {
        RpcId(1, MethodName.Chat, text);
    }

    public void SendInvited(long peer, string inviterId, string inviterName)
    {
        SendTo(peer, MethodName.ReceiveInvite, inviterId, inviterName);
    }

    public void SendParty(long peer, string leaderId, string[] memberIds, string[] names, int[] online)
    {
        SendTo(peer, MethodName.ReceiveParty, leaderId, memberIds, names, online);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Invite(long targetPeer)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.Invite, sender, targetPeer))
            {
                InviteRequested?.Invoke(sender, targetPeer);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Respond(string inviterId, bool accept)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.Respond, sender, inviterId, accept))
            {
                ResponseReceived?.Invoke(sender, inviterId, accept);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Leave()
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.Leave, sender))
            {
                LeaveRequested?.Invoke(sender);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Chat(string text)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.Chat, sender, text))
            {
                ChatRequested?.Invoke(sender, text);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveInvite(string inviterId, string inviterName)
    {
        InviteReceived?.Invoke(inviterId, inviterName);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveParty(string leaderId, string[] memberIds, string[] names, int[] online)
    {
        PartyReceived?.Invoke(leaderId, memberIds, names, online);
    }
}
