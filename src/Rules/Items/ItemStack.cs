namespace MmoGame3d.Rules.Items;

public class ItemStack
{
    public ItemStack(ItemType type, ItemTier tier, int quantity)
    {
        Type = type;
        Tier = tier;
        Quantity = quantity;
    }

    public ItemType Type { get; }
    public ItemTier Tier { get; }
    public int Quantity { get; set; }
}
