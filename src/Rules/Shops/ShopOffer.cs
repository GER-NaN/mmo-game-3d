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
