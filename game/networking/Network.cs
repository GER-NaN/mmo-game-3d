namespace MmoGame3d.Networking;

using System;
using System.Diagnostics;
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
public partial class Network : NetworkNode
{
    // Server side, before the world: (peer, protocol, license key) says who the account
    // is; then a character is made (peer, name, appearance) or played (peer, player id).
    public event Action<long, int, string>? HelloReceived;
    public event Action<long, string, string>? CreateCharacterRequested;
    public event Action<long, string>? LoginRequested;
    public event Action<long>? WorldReadyReceived;

    // Server side: a new look from the wardrobe.
    public event Action<long, string>? SetLookRequested;

    // Client side: the account's characters (ids, names, appearances, levels, career
    // titles) and a problem to show, or "".
    public event Action<string[], string[], string[], int[], string[], string>? CharactersReceived;

    public void SendHello(int protocol, string licenseKey)
    {
        RpcId(1, MethodName.Hello, protocol, licenseKey);
    }

    public void SendCreateCharacter(string name, string look)
    {
        RpcId(1, MethodName.CreateCharacter, name, look);
    }

    public void SendSetLook(string look)
    {
        RpcId(1, MethodName.SetLook, look);
    }

    public void SendCharacters(long peer, string[] ids, string[] names, string[] looks, int[] levels, string[] titles, string message)
    {
        SendTo(peer, MethodName.ReceiveCharacters, ids, names, looks, levels, titles, message);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Hello(int protocol, string licenseKey)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.Hello, sender, protocol))
            {
                HelloReceived?.Invoke(sender, protocol, licenseKey);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void CreateCharacter(string name, string look)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.CreateCharacter, sender, name, look))
            {
                CreateCharacterRequested?.Invoke(sender, name, look);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SetLook(string look)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.SetLook, sender, look))
            {
                SetLookRequested?.Invoke(sender, look);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveCharacters(string[] ids, string[] names, string[] looks, int[] levels, string[] titles, string message)
    {
        CharactersReceived?.Invoke(ids, names, looks, levels, titles, message);
    }

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
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.Walk, sender, direction, heading))
            {
                WalkRequested?.Invoke(sender, direction, heading);
            }
        }
    }

    // Reliable, so a lost packet cannot leave the body walking.
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void StopWalking(float heading)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.StopWalking, sender, heading))
            {
                StopRequested?.Invoke(sender, heading);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Jump()
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.Jump, sender))
            {
                JumpRequested?.Invoke(sender);
            }
        }
    }

    // Client side: (intent id, "" when approved or the refusal). Every intent, whatever
    // sent it (a buy, a drop, a gift), is answered here.
    public event Action<uint, string>? IntentAnswered;

    public void SendIntentAnswer(long peer, uint intentId, string refusal)
    {
        SendTo(peer, MethodName.ReceiveIntentAnswer, intentId, refusal);
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
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.Emote, sender, emoteId))
            {
                EmoteRequested?.Invoke(sender, emoteId);
            }
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
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.Interact, sender, interactableName))
            {
                InteractRequested?.Invoke(sender, interactableName);
            }
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
        SendTo(peer, MethodName.ReceiveZoneChanged, zoneId);
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
        SendTo(peer, MethodName.ReceiveClock, secondsOfDay);
    }

    // Unreliable: a lost one is replaced by the next, and the client counts on meanwhile.
    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void ReceiveClock(double secondsOfDay)
    {
        ClockReceived?.Invoke(secondsOfDay);
    }

    // Client side: (zone, cells) where the player has been in that zone; see Discovery.
    public event Action<string, byte[]>? MapReceived;

    public void SendMap(long peer, string zoneId, byte[] cells)
    {
        SendTo(peer, MethodName.ReceiveMap, zoneId, cells);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveMap(string zoneId, byte[] cells)
    {
        MapReceived?.Invoke(zoneId, cells);
    }

    // Server side: (peer, target player id, text) a private message.
    public event Action<long, string, string>? DirectRequested;

    // Client side: (the other player's id, their name, text, true when it came to you).
    public event Action<string, string, string, bool>? DirectReceived;

    public void SendDirect(string targetPlayerId, string text)
    {
        RpcId(1, MethodName.Direct, targetPlayerId, text);
    }

    public void SendDirectLine(long peer, string partnerId, string partnerName, string text, bool incoming)
    {
        SendTo(peer, MethodName.ReceiveDirect, partnerId, partnerName, text, incoming);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Direct(string targetPlayerId, string text)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.Direct, sender, targetPlayerId, text))
            {
                DirectRequested?.Invoke(sender, targetPlayerId, text);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveDirect(string partnerId, string partnerName, string text, bool incoming)
    {
        DirectReceived?.Invoke(partnerId, partnerName, text, incoming);
    }

    // Client side: a drone zapped someone, from and to (zone-local), to draw.
    public event Action<Vector3, Vector3>? DroneZapReceived;

    public void SendDroneZap(long peer, Vector3 from, Vector3 to)
    {
        SendTo(peer, MethodName.ReceiveDroneZap, from, to);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    private void ReceiveDroneZap(Vector3 from, Vector3 to)
    {
        DroneZapReceived?.Invoke(from, to);
    }

    // Server side: a player fires their EMP Emitter.
    public event Action<long>? EmpRequested;

    // Client side: an EMP pulse went off here (zone-local), to draw.
    public event Action<Vector3>? EmpPulseReceived;

    public void SendFireEmp()
    {
        RpcId(1, MethodName.FireEmp);
    }

    public void SendEmpPulse(long peer, Vector3 at)
    {
        SendTo(peer, MethodName.ReceiveEmpPulse, at);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void FireEmp()
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.FireEmp, sender))
            {
                EmpRequested?.Invoke(sender);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveEmpPulse(Vector3 at)
    {
        EmpPulseReceived?.Invoke(at);
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
        SendTo(peer, MethodName.ReceiveChatLine, sender, text, kind);
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
    private void ReceiveChatLine(string sender, string text, int kind)
    {
        ChatReceived?.Invoke(sender, text, kind);
    }

    public void SendInventory(long peer, int[] packed, int dollars, string[] ids, int[] meta, float[] charges)
    {
        SendTo(peer, MethodName.ReceiveInventory, packed, dollars, ids, meta, charges);
    }

    public void SendNotice(long peer, string text)
    {
        SendTo(peer, MethodName.ReceiveNotice, text);
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

    public void SendLogin(string playerId)
    {
        RpcId(1, MethodName.Login, playerId);
    }

    public void SendWorldReady()
    {
        RpcId(1, MethodName.WorldReady);
    }

    public void SendLoginAccepted(long peer, string zone, string displayName)
    {
        SendTo(peer, MethodName.Accept, zone, displayName);
    }

    public void SendLoginRefused(long peer, string reason)
    {
        SendTo(peer, MethodName.Refuse, reason);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Login(string playerId)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.Login, sender, playerId))
            {
                LoginRequested?.Invoke(sender, playerId);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void WorldReady()
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.WorldReady, sender))
            {
                WorldReadyReceived?.Invoke(sender);
            }
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
