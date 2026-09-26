namespace MmoGame3d.Networking;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;

/// <summary>
/// The terminal world's RPCs: going offline, the screen opening and closing, the roster
/// of who is online everywhere, and the town's repairs (taking a job, the town's state
/// and log for the apps). Going online is a use of a terminal, so it travels as a plain
/// interaction (Network.SendInteract).
/// </summary>
public partial class TerminalNetwork : NetworkNode
{
    // Server side.
    public event Action<long>? LeaveRequested;
    public event Action<long>? CrackStartRequested;
    public event Action<long, string>? CrackGuessRequested;

    // Client side: the code cracker: guesses so far, right-in-place and right-elsewhere
    // counts, the places that were right ("" unless the player may see them), guesses
    // left, and 0 playing / 1 cracked / 2 locked out.
    public event Action<string[], int[], int[], string[], int, int>? CrackReceived;

    public void SendCrackStart()
    {
        RpcId(1, MethodName.CrackStart);
    }

    public void SendCrackGuess(string guess)
    {
        RpcId(1, MethodName.CrackGuess, guess);
    }

    public void SendCrack(long peer, string[] guesses, int[] exact, int[] partial, string[] positions, int left, int status)
    {
        SendTo(peer, MethodName.ReceiveCrack, guesses, exact, partial, positions, left, status);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void CrackStart()
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.CrackStart, sender))
            {
                CrackStartRequested?.Invoke(sender);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void CrackGuess(string guess)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.CrackGuess, sender, guess))
            {
                CrackGuessRequested?.Invoke(sender, guess);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveCrack(string[] guesses, int[] exact, int[] partial, string[] positions, int left, int status)
    {
        CrackReceived?.Invoke(guesses, exact, partial, positions, left, status);
    }
    public event Action<long, string>? TakeJobRequested;

    // Client side: the town for the Town repairs and Town log apps: whether the street
    // lights work, whether this player has the job, and the log, newest first.
    // Then the same for the robo taxis' rootkit: clean, and whether this player has it.
    public event Action<bool, bool, bool, bool, string[]>? TownReceived;

    public void SendTakeJob(string jobId)
    {
        RpcId(1, MethodName.TakeJob, jobId);
    }

    public void SendTown(long peer, bool lightsWorking, bool lightsTaken, bool taxisClean, bool taxisTaken, string[] log)
    {
        SendTo(peer, MethodName.ReceiveTown, lightsWorking, lightsTaken, taxisClean, taxisTaken, log);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void TakeJob(string jobId)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.TakeJob, sender, jobId))
            {
                TakeJobRequested?.Invoke(sender, jobId);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveTown(bool lightsWorking, bool lightsTaken, bool taxisClean, bool taxisTaken, string[] log)
    {
        TownReceived?.Invoke(lightsWorking, lightsTaken, taxisClean, taxisTaken, log);
    }

    // Agent Defense: the client asks for a run and gets its seed; at the end it sends
    // its presses, packed (AgentDefense.Pack).
    public event Action<long>? DefenseStartRequested;
    public event Action<long, int[]>? DefenseFinishRequested;
    // (seed, length in ms).
    public event Action<int, int>? DefenseSeedReceived;

    public void SendDefenseStart()
    {
        RpcId(1, MethodName.DefenseStart);
    }

    public void SendDefenseFinish(int[] presses)
    {
        RpcId(1, MethodName.DefenseFinish, presses);
    }

    public void SendDefenseSeed(long peer, int seed, int lengthMs)
    {
        SendTo(peer, MethodName.ReceiveDefenseSeed, seed, lengthMs);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void DefenseStart()
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.DefenseStart, sender))
            {
                DefenseStartRequested?.Invoke(sender);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void DefenseFinish(int[] presses)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.DefenseFinish, sender, presses.Length))
            {
                DefenseFinishRequested?.Invoke(sender, presses);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveDefenseSeed(int seed, int lengthMs)
    {
        DefenseSeedReceived?.Invoke(seed, lengthMs);
    }

    // Server side: (peer, drone name) reported on Old Town's cameras.
    public event Action<long, string>? SpotRequested;

    public void SendSpot(string droneName)
    {
        RpcId(1, MethodName.Spot, droneName);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Spot(string droneName)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.Spot, sender, droneName))
            {
                SpotRequested?.Invoke(sender, droneName);
            }
        }
    }

    // Client side: an objective's leaderboard: the top lines, then "Your best: ...".
    public event Action<string, string[]>? BoardReceived;

    public void SendBoard(long peer, string objective, string[] lines)
    {
        SendTo(peer, MethodName.ReceiveBoard, objective, lines);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveBoard(string objective, string[] lines)
    {
        BoardReceived?.Invoke(objective, lines);
    }

    // Client side: the status board, newest first.
    public event Action<string[]>? StatusReceived;

    public void SendStatus(long peer, string[] lines)
    {
        SendTo(peer, MethodName.ReceiveStatus, lines);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveStatus(string[] lines)
    {
        StatusReceived?.Invoke(lines);
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
        SendTo(peer, MethodName.ReceiveOpened, terminalType, terminalName);
    }

    public void SendClosed(long peer)
    {
        SendTo(peer, MethodName.ReceiveClosed);
    }

    public void SendRoster(long peer, string[] names, string[] zones, int[] online)
    {
        SendTo(peer, MethodName.ReceiveRoster, names, zones, online);
    }

    public void SendRoster(IReadOnlyList<long> peers, string[] names, string[] zones, int[] online)
    {
        SendToMany(peers, MethodName.ReceiveRoster, names, zones, online);
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
