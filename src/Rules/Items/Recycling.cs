namespace MmoGame3d.Rules.Items;

/// <summary>
/// The recycler (world.md 9: nearly everything is sellable; go to a recycler and
/// recycle it for some currency). Something goes in and pocket change comes out; a
/// thing with parts goes with its parts (a phone with its battery). Worn things are
/// refused, so nobody recycles the phone in their hand by mistake. Placeholder prices.
/// </summary>
public static class Recycling
{
    private static readonly Dictionary<ItemType, int> BaseValue = new Dictionary<ItemType, int>
    {
        { ItemType.GpuCore, 6 },
        { ItemType.RamStick, 2 },
        { ItemType.Phone, 5 },
        { ItemType.Battery, 1 },
        { ItemType.EmpEmitter, 3 },
        { ItemType.PottedMonstera, 2 },
        { ItemType.PottedPothos, 2 },
        { ItemType.PottedSnakePlant, 2 },
        { ItemType.PottedYucca, 2 },
        { ItemType.PottedZzPlant, 2 },
    };

    // Each tier up doubles it.
    public static int ValueOf(ItemType type, ItemTier tier)
    {
        int value;

        if (!BaseValue.TryGetValue(type, out value))
        {
            return 0;
        }

        return value << (int)tier;
    }

    // One of a stack. Null when done, or the reason not; dollars is what it paid.
    public static string? RecycleOne(Inventory stacks, ItemType type, ItemTier tier, out int dollars)
    {
        dollars = 0;

        if (!stacks.TryRemove(type, tier, 1))
        {
            return "You have none of those.";
        }

        dollars = ValueOf(type, tier);
        return null;
    }

    public static string? RecycleThing(Belongings mine, Guid id, out int dollars)
    {
        dollars = 0;
        ItemInstance? thing = mine.Find(id);

        if (thing == null || thing.ParentId != null)
        {
            return "That is not loose in your bag.";
        }

        if (thing.Slot != null)
        {
            return "Unequip it first.";
        }

        List<ItemInstance> gone = new List<ItemInstance> { thing };

        foreach (ItemInstance inside in mine.Instances)
        {
            if (inside.ParentId == thing.Id)
            {
                gone.Add(inside);
            }
        }

        foreach (ItemInstance item in gone)
        {
            dollars += ValueOf(item.Type, item.Tier);
            mine.Instances.Remove(item);
        }

        return null;
    }
}
