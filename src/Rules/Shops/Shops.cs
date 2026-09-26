namespace MmoGame3d.Rules.Shops;

using MmoGame3d.Rules.Items;

// One thing a shop sells, at a price in dollars.
public class ShopOffer
{
    public ShopOffer(ItemType type, ItemTier tier, int price)
    {
        Type = type;
        Tier = tier;
        Price = price;
    }

    public ItemType Type { get; }
    public ItemTier Tier { get; }
    public int Price { get; }
}

/// <summary>
/// What each shop sells. Both sides build the same list from this code, so a buy names
/// an offer by its index and the server looks the price up itself: a client never says
/// what something costs.
/// </summary>
public static class Shops
{
    public const string Electronics = "electronics";

    // A new player has this much pocket change.
    public const int StartingDollars = 10;

    // The first-playable shop: a battery a new player can just afford, and a few things
    // above their means that show and refuse. Placeholder prices.
    private static readonly Dictionary<string, List<ShopOffer>> OffersByShop = new Dictionary<string, List<ShopOffer>>
    {
        {
            Electronics,
            new List<ShopOffer>
            {
                new ShopOffer(ItemType.Battery, ItemTier.Standard, 8),
                new ShopOffer(ItemType.RamStick, ItemTier.Enhanced, 25),
                new ShopOffer(ItemType.GpuCore, ItemTier.Advanced, 60),
                new ShopOffer(ItemType.Phone, ItemTier.Standard, 120),
            }
        },
    };

    public static IReadOnlyList<ShopOffer> Offers(string shopId)
    {
        List<ShopOffer>? offers;
        return OffersByShop.TryGetValue(shopId, out offers) ? offers : new List<ShopOffer>();
    }

    public static ShopOffer? Offer(string shopId, int index)
    {
        IReadOnlyList<ShopOffer> offers = Offers(shopId);
        return index >= 0 && index < offers.Count ? offers[index] : null;
    }

    // Null when the buy may go ahead, else the reason, worded for the player.
    public static string? CannotBuy(int dollars, ShopOffer offer)
    {
        if (dollars < offer.Price)
        {
            return "You cannot afford that: it costs $" + offer.Price + " and you have $" + dollars + ".";
        }

        return null;
    }
}
