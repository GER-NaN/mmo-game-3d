namespace MmoGame3d.Rules.Items;

/// <summary>
/// One thing with an identity and state of its own: this phone, this battery at 43%.
/// It is loose in the bag (no parent, no slot), equipped by the player (no parent, a
/// slot), or inside another item (a parent and the slot it fills there).
/// </summary>
public class ItemInstance
{
    public ItemInstance(Guid id, ItemType type, ItemTier tier)
    {
        Id = id;
        Type = type;
        Tier = tier;
    }

    public Guid Id { get; }
    public ItemType Type { get; }
    public ItemTier Tier { get; }
    public Guid? ParentId { get; set; }
    public SlotType? Slot { get; set; }

    // 0 to 1, for a battery; null for anything without a charge.
    public float? Charge { get; set; }

    public bool IsLoose
    {
        get { return ParentId == null && Slot == null; }
    }
}
