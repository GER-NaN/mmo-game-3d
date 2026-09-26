namespace MmoGame3d.Networking;

using System;
using Godot;

/// <summary>
/// The shop RPCs. A buy is an intent: it carries an id the client chose, the server acts
/// on an id once and answers every id (on Network, where all intents are answered), and
/// the client resends an id it has no answer for. A buy names an offer by its index in
/// the shop's list; the price is looked up on the server, never sent.
/// </summary>
public partial class ShopNetwork : Node
{
    // Server side: (peer, intent id, shop id, offer index).
    public event Action<long, uint, string, int>? BuyRequested;

    // Client side: a shopkeeper was used; the shop's id. The offers are in the rules.
    public event Action<string>? ShopOpened;

    public void SendBuy(uint intentId, string shopId, int offerIndex)
    {
        RpcId(1, MethodName.Buy, intentId, shopId, offerIndex);
    }

    public void SendShopOpened(long peer, string shopId)
    {
        RpcId(peer, MethodName.ReceiveShopOpened, shopId);
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
}
