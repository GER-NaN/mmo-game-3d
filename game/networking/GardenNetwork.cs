namespace MmoGame3d.Networking;

using System;
using System.Diagnostics;
using Godot;

/// <summary>
/// The greenhouse: finishing a house plant (an intent, answered on Network), and what
/// the player is told back: the table opened, the plant made, a plant's provenance card.
/// A plant travels as its design text (see PlantDesign), checked on the server.
/// </summary>
public partial class GardenNetwork : NetworkNode
{
    // Server side: (peer, intent id, design, name).
    public event Action<long, uint, string, string>? CompleteRequested;

    // Client side.
    public event Action? GardenOpened;

    // (plant number, name, the reward's name).
    public event Action<long, string, string>? PlantMade;

    // (plant number, name, creator, made on, design, history lines).
    public event Action<long, string, string, string, string, string[]>? CardReceived;

    public void SendComplete(uint intentId, string design, string name)
    {
        RpcId(1, MethodName.Complete, intentId, design, name);
    }

    public void SendGardenOpened(long peer)
    {
        SendTo(peer, MethodName.ReceiveGardenOpened);
    }

    public void SendPlantMade(long peer, long plantId, string name, string reward)
    {
        SendTo(peer, MethodName.ReceivePlantMade, plantId, name, reward);
    }

    public void SendCard(long peer, long plantId, string name, string creator, string madeOn, string design, string[] history)
    {
        SendTo(peer, MethodName.ReceiveCard, plantId, name, creator, madeOn, design, history);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Complete(uint intentId, string design, string name)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.Complete, sender, intentId, design, name))
            {
                CompleteRequested?.Invoke(sender, intentId, design, name);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveGardenOpened()
    {
        GardenOpened?.Invoke();
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceivePlantMade(long plantId, string name, string reward)
    {
        PlantMade?.Invoke(plantId, name, reward);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveCard(long plantId, string name, string creator, string madeOn, string design, string[] history)
    {
        CardReceived?.Invoke(plantId, name, creator, madeOn, design, history);
    }
}
