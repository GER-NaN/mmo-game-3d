namespace MmoGame3d.Networking;

using System;
using Godot;

/// <summary>
/// The shop RPCs, and the answers to intents. A buy is an intent: it carries an id the
/// client chose, the server acts on an id once and answers every id, and the client
/// resends an id it has no answer for (see IntentLedger). A buy names an offer by its
/// index in the shop's list; the price is looked up on the server, never sent.
/// </summary>
public partial class ShopNetwork : Node
{
    // Server side: (peer, intent id, shop id, offer index).
    public event Action<long, uint, string, int>? BuyRequested;

    // Client side: a shopkeeper was used; the shop's id. The offers are in the rules.
    public event Action<string>? ShopOpened;

    // Client side: (intent id, "" when approved or the refusal).
    public event Action<uint, string>? IntentAnswered;

    public void SendBuy(uint intentId, string shopId, int offerIndex)
    {
        RpcId(1, MethodName.Buy, intentId, shopId, offerIndex);
    }

    public void SendShopOpened(long peer, string shopId)
    {
        RpcId(peer, MethodName.ReceiveShopOpened, shopId);
    }

    public void SendIntentAnswer(long peer, uint intentId, string refusal)
    {
        RpcId(peer, MethodName.ReceiveIntentAnswer, intentId, refusal);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Buy(uint intentId, string shopId, int offerIndex)
    {
        if (Multiplayer.IsServer())
        {
            BuyRequested?.Invoke(Multiplayer.GetRemoteSenderId(), intentId, shopId, offerIndex);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveShopOpened(string shopId)
    {
        ShopOpened?.Invoke(shopId);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveIntentAnswer(uint intentId, string refusal)
    {
        IntentAnswered?.Invoke(intentId, refusal);
    }
}
