namespace MmoGame3d.Rules.Items;

/// <summary>
/// Weighted choice of what lies on the ground: an entry's weight against the total is
/// its chance. The random source is passed in, so a test can fix it.
/// </summary>
public class LootTable
{
    private readonly List<LootEntry> _entries = new List<LootEntry>();
    private int _totalWeight;

    // What the town's ground holds. Placeholder weights: commons often, elites rarely.
    public static LootTable Ground()
    {
        LootTable table = new LootTable();
        table.Add(new LootEntry(ItemType.GpuCore, ItemTier.Standard, 30, 1, 3));
        table.Add(new LootEntry(ItemType.GpuCore, ItemTier.Enhanced, 12, 1, 2));
        table.Add(new LootEntry(ItemType.GpuCore, ItemTier.Advanced, 4, 1, 1));
        table.Add(new LootEntry(ItemType.GpuCore, ItemTier.Elite, 1, 1, 1));
        table.Add(new LootEntry(ItemType.RamStick, ItemTier.Standard, 30, 1, 4));
        table.Add(new LootEntry(ItemType.RamStick, ItemTier.Enhanced, 12, 1, 2));
        table.Add(new LootEntry(ItemType.RamStick, ItemTier.Advanced, 4, 1, 1));
        table.Add(new LootEntry(ItemType.RamStick, ItemTier.Elite, 1, 1, 1));
        return table;
    }

    public void Add(LootEntry entry)
    {
        if (entry.Weight <= 0 || entry.MinQuantity <= 0 || entry.MaxQuantity < entry.MinQuantity)
        {
            throw new ArgumentException("A loot entry needs a positive weight and a quantity range of at least one.", nameof(entry));
        }

        _entries.Add(entry);
        _totalWeight += entry.Weight;
    }

    public LootRoll Roll(Random random)
    {
        int pick = random.Next(_totalWeight);

        foreach (LootEntry entry in _entries)
        {
            if (pick < entry.Weight)
            {
                return new LootRoll(entry.Type, entry.Tier, random.Next(entry.MinQuantity, entry.MaxQuantity + 1));
            }

            pick -= entry.Weight;
        }

        throw new InvalidOperationException("The loot table is empty.");
    }
}

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
