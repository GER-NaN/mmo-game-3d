namespace MmoGame3d.Networking;

using System;
using Godot;

/// <summary>
/// The terminal world's RPCs: going offline, the screen opening and closing, and the
/// roster of who is online everywhere. Going online is a use of a terminal, so it
/// travels as a plain interaction (Network.SendInteract).
/// </summary>
public partial class TerminalNetwork : Node
{
    // Server side.
    public event Action<long>? LeaveRequested;

    // Client side: the door's TerminalType, and the terminal's name.
    public event Action<int, string>? Opened;
    public event Action? Closed;

    // Client side: everyone in the world (names, zones), and whether each is online.
    public event Action<string[], string[], int[]>? RosterReceived;

    public void SendLeave()
    {
        RpcId(1, MethodName.Leave);
    }

    public void SendOpened(long peer, int terminalType, string terminalName)
    {
        RpcId(peer, MethodName.ReceiveOpened, terminalType, terminalName);
    }

    public void SendClosed(long peer)
    {
        RpcId(peer, MethodName.ReceiveClosed);
    }

    public void SendRoster(long peer, string[] names, string[] zones, int[] online)
    {
        RpcId(peer, MethodName.ReceiveRoster, names, zones, online);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Leave()
    {
        if (Multiplayer.IsServer())
        {
            LeaveRequested?.Invoke(Multiplayer.GetRemoteSenderId());
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveOpened(int terminalType, string terminalName)
    {
        Opened?.Invoke(terminalType, terminalName);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveClosed()
    {
        Closed?.Invoke();
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveRoster(string[] names, string[] zones, int[] online)
    {
        RosterReceived?.Invoke(names, zones, online);
    }
}
