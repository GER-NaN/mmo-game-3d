namespace MmoGame3d.Networking;

using System;
using Godot;

/// <summary>
/// The terminal world's RPCs: going offline, the screen opening and closing, the roster
/// of who is online everywhere, and the town's repairs (taking a job, the town's state
/// and log for the apps). Going online is a use of a terminal, so it travels as a plain
/// interaction (Network.SendInteract).
/// </summary>
public partial class TerminalNetwork : Node
{
    // Server side.
    public event Action<long>? LeaveRequested;
    public event Action<long, string>? TakeJobRequested;

    // Client side: the town for the Town repairs and Town log apps: whether the street
    // lights work, whether this player has the job, and the log, newest first.
    public event Action<bool, bool, string[]>? TownReceived;

    public void SendTakeJob(string jobId)
    {
        RpcId(1, MethodName.TakeJob, jobId);
    }

    public void SendTown(long peer, bool lightsWorking, bool jobTaken, string[] log)
    {
        RpcId(peer, MethodName.ReceiveTown, lightsWorking, jobTaken, log);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void TakeJob(string jobId)
    {
        if (Multiplayer.IsServer())
        {
            TakeJobRequested?.Invoke(Multiplayer.GetRemoteSenderId(), jobId);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveTown(bool lightsWorking, bool jobTaken, string[] log)
    {
        TownReceived?.Invoke(lightsWorking, jobTaken, log);
    }

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
