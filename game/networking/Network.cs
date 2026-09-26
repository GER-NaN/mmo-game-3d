namespace MmoGame3d.Networking;

using System;
using Godot;

/// <summary>
/// The session RPCs: login, entering the world, the private state (bag, notices, the
/// time), chat, and using things. The node sits at the same path on server and client,
/// which is how an RPC finds its way. Each RPC only raises an event; ServerGame and
/// ClientGame decide what it means. Features with several RPCs of their own get their
/// own node (PartyNetwork, TerminalNetwork).
///
/// Who may call what: requests come from any peer and are handled only on the server,
/// which knows the sender from the connection. The answers are Authority RPCs, so only
/// the server can send them.
/// </summary>
public partial class Network : Node
{
    // Server side: (peer, protocol, license key, display name).
    public event Action<long, int, string, string>? LoginRequested;
    public event Action<long>? WorldReadyReceived;

    // Server side: walking, sent here rather than to the body, because a body leaves its
    // zone at once when its player goes through a door, and a walk still on its way would
    // arrive for a node that is gone. The server hands it to the session's body, if any.
    public event Action<long, Vector2, float>? WalkRequested;
    public event Action<long, float>? StopRequested;
    public event Action<long>? JumpRequested;

    public void SendWalk(Vector2 direction, float heading)
    {
        RpcId(1, MethodName.Walk, direction, heading);
    }

    public void SendStop(float heading)
    {
        RpcId(1, MethodName.StopWalking, heading);
    }

    public void SendJump()
    {
        RpcId(1, MethodName.Jump);
    }

    // Unreliable but ordered: a lost walk is replaced by the next one 50 ms later, and an
    // old one never overtakes a newer one.
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    private void Walk(Vector2 direction, float heading)
    {
        if (Multiplayer.IsServer())
        {
            WalkRequested?.Invoke(Multiplayer.GetRemoteSenderId(), direction, heading);
        }
    }

    // Reliable, so a lost packet cannot leave the body walking.
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void StopWalking(float heading)
    {
        if (Multiplayer.IsServer())
        {
            StopRequested?.Invoke(Multiplayer.GetRemoteSenderId(), heading);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Jump()
    {
        if (Multiplayer.IsServer())
        {
            JumpRequested?.Invoke(Multiplayer.GetRemoteSenderId());
        }
    }

    // Client side: (intent id, "" when approved or the refusal). Every intent, whatever
    // sent it (a buy, a drop, a gift), is answered here.
    public event Action<uint, string>? IntentAnswered;

    public void SendIntentAnswer(long peer, uint intentId, string refusal)
    {
        RpcId(peer, MethodName.ReceiveIntentAnswer, intentId, refusal);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveIntentAnswer(uint intentId, string refusal)
    {
        IntentAnswered?.Invoke(intentId, refusal);
    }

    // Server side: (peer, emote id) typed as a chat command.
    public event Action<long, string>? EmoteRequested;

    public void SendEmote(string emoteId)
    {
        RpcId(1, MethodName.Emote, emoteId);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Emote(string emoteId)
    {
        if (Multiplayer.IsServer())
        {
            EmoteRequested?.Invoke(Multiplayer.GetRemoteSenderId(), emoteId);
        }
    }

    // Server side: (peer, name of the interactable) the player wants to use.
    public event Action<long, string>? InteractRequested;

    public void SendInteract(string interactableName)
    {
        RpcId(1, MethodName.Interact, interactableName);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Interact(string interactableName)
    {
        if (Multiplayer.IsServer())
        {
            InteractRequested?.Invoke(Multiplayer.GetRemoteSenderId(), interactableName);
        }
    }

    // Client side: (zone, display name), and the refusal's reason.
    public event Action<string, string>? LoginAccepted;
    public event Action<string>? LoginRefused;

    // Client side: the whole bag, sent to its owner only: the stacks packed (see
    // InventoryWire), the pocket change, and the instances (see InstanceWire).
    public event Action<int[], int, string[], int[], float[]>? InventoryReceived;

    // Client side: a short line for the player ("Picked up 2 GPU core").
    public event Action<string>? NoticeReceived;

    // Client side: the player walked through a door into this zone. The client loads it
    // and says WorldReady again, as at login.
    public event Action<string>? ZoneChanged;

    public void SendZoneChanged(long peer, string zoneId)
    {
        RpcId(peer, MethodName.ReceiveZoneChanged, zoneId);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveZoneChanged(string zoneId)
    {
        ZoneChanged?.Invoke(zoneId);
    }

    // Client side: seconds since the world's midnight.
    public event Action<double>? ClockReceived;

    public void SendClock(long peer, double secondsOfDay)
    {
        RpcId(peer, MethodName.ReceiveClock, secondsOfDay);
    }

    // Unreliable: a lost one is replaced by the next, and the client counts on meanwhile.
    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void ReceiveClock(double secondsOfDay)
    {
        ClockReceived?.Invoke(secondsOfDay);
    }

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

    public void SendInventory(long peer, int[] packed, int dollars, string[] ids, int[] meta, float[] charges)
    {
        RpcId(peer, MethodName.ReceiveInventory, packed, dollars, ids, meta, charges);
    }

    public void SendNotice(long peer, string text)
    {
        RpcId(peer, MethodName.ReceiveNotice, text);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveInventory(int[] packed, int dollars, string[] ids, int[] meta, float[] charges)
    {
        InventoryReceived?.Invoke(packed, dollars, ids, meta, charges);
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
