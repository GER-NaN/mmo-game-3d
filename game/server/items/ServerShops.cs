namespace MmoGame3d.Server;

using System;
using Godot;
using MmoGame3d.Networking;
using MmoGame3d.Players;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Shops;
using MmoGame3d.Vendors;

/// <summary>
/// Buying from shopkeepers. Using a shopkeeper opens their shop for the player; a buy
/// is then taken only while that shopkeeper is in reach, so a client cannot shop from
/// across town. Every buy is an intent: acted on once per id, and always answered.
/// </summary>
public class ServerShops
{
    // A little slack over the shopkeeper's reach, for the body having moved.
    private const float ReachSlack = 1.5f;

    private readonly ShopNetwork _network;
    private readonly Network _session;
    private readonly Action<Session> _bagChanged;
    private readonly ServerIntents _intents;

    public ServerShops(ShopNetwork network, Network session, ServerIntents intents, Action<Session> bagChanged)
    {
        _network = network;
        _session = session;
        _intents = intents;
        _bagChanged = bagChanged;
    }

    public void Open(Session session, Vendor vendor)
    {
        session.OpenVendor = vendor;
        _network.SendShopOpened(session.PeerId, vendor.ShopId);
    }

    public void Buy(Session session, uint intentId, string shopId, int offerIndex)
    {
        _intents.Run(session, intentId, () => Decide(session, shopId, offerIndex));
    }

    // "" when bought, else the refusal. Only an approved buy changes anything.
    private string Decide(Session session, string shopId, int offerIndex)
    {
        Vendor? vendor = session.OpenVendor;
        Player? body = session.Body;
        bool atShop = vendor != null && GodotObject.IsInstanceValid(vendor) && body != null
            && vendor.ShopId == shopId && vendor.IsInReach(body.GlobalPosition, ReachSlack);

        if (!atShop)
        {
            return "You need to be at the shop to buy.";
        }

        ShopOffer? offer = Shops.Offer(shopId, offerIndex);

        if (offer == null)
        {
            return "That is not for sale here.";
        }

        string? refusal = Shops.CannotBuy(session.Dollars, offer);

        if (refusal != null)
        {
            return refusal;
        }

        session.Dollars -= offer.Price;

        // A thing with an identity (a phone, an emitter) is bought as itself, not as a
        // stack: only then can it be equipped.
        if (ItemCatalog.Get(offer.Type).Stackable)
        {
            session.Inventory!.Add(offer.Type, offer.Tier, 1);
        }
        else
        {
            session.Instances.Add(new ItemInstance(Guid.NewGuid(), offer.Type, offer.Tier));
        }

        _bagChanged(session);
        _session.SendNotice(session.PeerId, "Bought " + ItemCatalog.Describe(offer.Type, offer.Tier) + " for $" + offer.Price + ".");
        return "";
    }
}
