namespace MmoGame3d.Networking;

using System;
using System.Diagnostics;
using Godot;

/// <summary>
/// The item RPCs: equip and unequip, going online by phone, the workbench, and dropping
/// and giving. Instances are named by their id, which only their owner ever sees. The
/// equipment ones spend nothing and carry no intent id: the server answers a refusal
/// with a notice and a change with the new bag. Drop and give take things away, so they
/// are intents, answered on Network.
/// </summary>
public partial class ItemNetwork : NetworkNode
{
    // Server side.
    public event Action<long, string>? EquipRequested;
    public event Action<long, string>? UnequipRequested;
    public event Action<long>? PhoneRequested;
    public event Action<long, string>? RemoveBatteryRequested;
    public event Action<long, string>? InsertBatteryRequested;

    // Server side, intents: (peer, intent id, type, tier, quantity), and for a gift also
    // the target's peer and any dollars.
    public event Action<long, uint, int, int, int>? DropRequested;
    public event Action<long, uint, long, int, int, int, int>? GiveRequested;

    public void SendDrop(uint intentId, int type, int tier, int quantity)
    {
        RpcId(1, MethodName.Drop, intentId, type, tier, quantity);
    }

    public void SendGive(uint intentId, long targetPeer, int type, int tier, int quantity, int dollars)
    {
        RpcId(1, MethodName.Give, intentId, targetPeer, type, tier, quantity, dollars);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Drop(uint intentId, int type, int tier, int quantity)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.Drop, sender, intentId, type, tier, quantity))
            {
                DropRequested?.Invoke(sender, intentId, type, tier, quantity);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Give(uint intentId, long targetPeer, int type, int tier, int quantity, int dollars)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.Give, sender, intentId, targetPeer, type, tier, quantity, dollars))
            {
                GiveRequested?.Invoke(sender, intentId, targetPeer, type, tier, quantity, dollars);
            }
        }
    }

    // Client side: a workbench was used, or a repair pack opened.
    public event Action? WorkbenchOpened;

    // Server side: an engineer opens their repair pack.
    public event Action<long>? RepairPackRequested;

    public void SendOpenRepairPack()
    {
        RpcId(1, MethodName.OpenRepairPack);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void OpenRepairPack()
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.OpenRepairPack, sender))
            {
                RepairPackRequested?.Invoke(sender);
            }
        }
    }

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
        SendTo(peer, MethodName.ReceiveWorkbenchOpened);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Equip(string instanceId)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.Equip, sender, instanceId))
            {
                EquipRequested?.Invoke(sender, instanceId);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Unequip(string instanceId)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.Unequip, sender, instanceId))
            {
                UnequipRequested?.Invoke(sender, instanceId);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void UsePhone()
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.UsePhone, sender))
            {
                PhoneRequested?.Invoke(sender);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RemoveBattery(string phoneId)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.RemoveBattery, sender, phoneId))
            {
                RemoveBatteryRequested?.Invoke(sender, phoneId);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void InsertBattery(string phoneId)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.InsertBattery, sender, phoneId))
            {
                InsertBatteryRequested?.Invoke(sender, phoneId);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveWorkbenchOpened()
    {
        WorkbenchOpened?.Invoke();
    }
}
