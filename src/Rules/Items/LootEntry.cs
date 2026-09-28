namespace MmoGame3d.Rules.Items;

public class LootEntry
{
    public LootEntry(ItemType type, ItemTier tier, int weight, int minQuantity, int maxQuantity)
    {
        Type = type;
        Tier = tier;
        Weight = weight;
        MinQuantity = minQuantity;
        MaxQuantity = maxQuantity;
    }

    public ItemType Type { get; }
    public ItemTier Tier { get; }
    public int Weight { get; }
    public int MinQuantity { get; }
    public int MaxQuantity { get; }
}
