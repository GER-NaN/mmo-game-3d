namespace MmoGame3d.Rules.Items;

public class LootRoll
{
    public LootRoll(ItemType type, ItemTier tier, int quantity)
    {
        Type = type;
        Tier = tier;
        Quantity = quantity;
    }

    public ItemType Type { get; }
    public ItemTier Tier { get; }
    public int Quantity { get; }
}
