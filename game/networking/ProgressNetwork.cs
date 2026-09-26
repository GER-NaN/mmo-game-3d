namespace MmoGame3d.Networking;

using System;
using System.Diagnostics;
using Godot;

/// <summary>
/// Skills and careers: the owner's own progress, and the college (the Class, enrolling,
/// ranking up). Progress goes only to its owner; the career title others see is synced
/// on the body.
/// </summary>
public partial class ProgressNetwork : NetworkNode
{
    // Server side: (peer), (peer, career id), (peer).
    public event Action<long>? TakeClassRequested;
    public event Action<long, int>? EnrollRequested;
    public event Action<long>? RankUpRequested;

    // Client side: skill ids and their experience; career id (-1 for none), career
    // experience, rank, whether the Class is taken, the player level.
    public event Action<int[], long[], int, long, int, bool, int>? ProgressReceived;

    // Client side: a college person was used ("registrar" or "professor").
    public event Action<string>? CollegeOpened;

    public void SendTakeClass()
    {
        RpcId(1, MethodName.TakeClass);
    }

    public void SendEnroll(int careerId)
    {
        RpcId(1, MethodName.Enroll, careerId);
    }

    public void SendRankUp()
    {
        RpcId(1, MethodName.RankUp);
    }

    public void SendProgress(long peer, int[] skills, long[] xp, int career, long careerXp, int rank, bool classTaken, int level)
    {
        SendTo(peer, MethodName.ReceiveProgress, skills, xp, career, careerXp, rank, classTaken, level);
    }

    // Client side: the ids of the achievements earned.
    public event Action<string[]>? AchievementsReceived;

    public void SendAchievements(long peer, string[] earned)
    {
        SendTo(peer, MethodName.ReceiveAchievements, earned);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveAchievements(string[] earned)
    {
        AchievementsReceived?.Invoke(earned);
    }

    public void SendCollegeOpened(long peer, string who)
    {
        SendTo(peer, MethodName.ReceiveCollegeOpened, who);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void TakeClass()
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.TakeClass, sender))
            {
                TakeClassRequested?.Invoke(sender);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Enroll(int careerId)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.Enroll, sender, careerId))
            {
                EnrollRequested?.Invoke(sender, careerId);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RankUp()
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.RankUp, sender))
            {
                RankUpRequested?.Invoke(sender);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveProgress(int[] skills, long[] xp, int career, long careerXp, int rank, bool classTaken, int level)
    {
        ProgressReceived?.Invoke(skills, xp, career, careerXp, rank, classTaken, level);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveCollegeOpened(string who)
    {
        CollegeOpened?.Invoke(who);
    }
}
