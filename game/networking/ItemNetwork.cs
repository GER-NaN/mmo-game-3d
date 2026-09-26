namespace MmoGame3d.Networking;

using System;
using Godot;

/// <summary>
/// The equipment RPCs: equip and unequip, going online by phone, and the workbench.
/// Instances are named by their id, which only their owner ever sees. None of these
/// spend anything, so they carry no intent id; the server answers a refusal with a
/// notice and a change with the new bag.
/// </summary>
public partial class ItemNetwork : Node
{
    // Server side.
    public event Action<long, string>? EquipRequested;
    public event Action<long, string>? UnequipRequested;
    public event Action<long>? PhoneRequested;
    public event Action<long, string>? RemoveBatteryRequested;
    public event Action<long, string>? InsertBatteryRequested;

    // Client side: a workbench was used.
    public event Action? WorkbenchOpened;

    public void SendEquip(string instanceId)
    {
        RpcId(1, MethodName.Equip, instanceId);
    }

    public void SendUnequip(string instanceId)
    {
        RpcId(1, MethodName.Unequip, instanceId);
    }

    public void SendUsePhone()
    {
        RpcId(1, MethodName.UsePhone);
    }

    public void SendRemoveBattery(string phoneId)
    {
        RpcId(1, MethodName.RemoveBattery, phoneId);
    }

    public void SendInsertBattery(string phoneId)
    {
        RpcId(1, MethodName.InsertBattery, phoneId);
    }

    public void SendWorkbenchOpened(long peer)
    {
        RpcId(peer, MethodName.ReceiveWorkbenchOpened);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Equip(string instanceId)
    {
        if (Multiplayer.IsServer())
        {
            EquipRequested?.Invoke(Multiplayer.GetRemoteSenderId(), instanceId);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Unequip(string instanceId)
    {
        if (Multiplayer.IsServer())
        {
            UnequipRequested?.Invoke(Multiplayer.GetRemoteSenderId(), instanceId);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void UsePhone()
    {
        if (Multiplayer.IsServer())
        {
            PhoneRequested?.Invoke(Multiplayer.GetRemoteSenderId());
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RemoveBattery(string phoneId)
    {
        if (Multiplayer.IsServer())
        {
            RemoveBatteryRequested?.Invoke(Multiplayer.GetRemoteSenderId(), phoneId);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void InsertBattery(string phoneId)
    {
        if (Multiplayer.IsServer())
        {
            InsertBatteryRequested?.Invoke(Multiplayer.GetRemoteSenderId(), phoneId);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveWorkbenchOpened()
    {
        WorkbenchOpened?.Invoke();
    }
}
