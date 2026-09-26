namespace MmoGame3d.Rules.Items;

// A place an item goes: a player's equipment slots, and the slots inside an item (a
// phone's battery). One mechanism at two levels, as in mmo-game.
public enum SlotType
{
    Device,
    Battery,
}

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

/// <summary>
/// Everything a player has: the stacks, the instances, and the pocket change. The
/// equipment rules live here so client and server read them the same way; the server
/// is the one that acts. Every change returns null when it happened, or the reason it
/// did not, worded for the player.
/// </summary>
public class Belongings
{
    // A starter phone comes with its battery at this charge (first-playable: 10%).
    public const float StarterCharge = 0.1f;

    public Belongings(Inventory stacks, List<ItemInstance> instances)
    {
        Stacks = stacks;
        Instances = instances;
    }

    public Inventory Stacks { get; }
    public List<ItemInstance> Instances { get; }

    public static List<ItemInstance> StarterKit()
    {
        ItemInstance phone = new ItemInstance(Guid.NewGuid(), ItemType.Phone, ItemTier.Standard);
        ItemInstance battery = new ItemInstance(Guid.NewGuid(), ItemType.Battery, ItemTier.Standard)
        {
            ParentId = phone.Id,
            Slot = SlotType.Battery,
            Charge = StarterCharge,
        };

        return new List<ItemInstance> { phone, battery };
    }

    public ItemInstance? Find(Guid id)
    {
        foreach (ItemInstance instance in Instances)
        {
            if (instance.Id == id)
            {
                return instance;
            }
        }

        return null;
    }

    // What the player has equipped in a slot of their own.
    public ItemInstance? Equipped(SlotType slot)
    {
        foreach (ItemInstance instance in Instances)
        {
            if (instance.ParentId == null && instance.Slot == slot)
            {
                return instance;
            }
        }

        return null;
    }

    public ItemInstance? Inside(ItemInstance parent, SlotType slot)
    {
        foreach (ItemInstance instance in Instances)
        {
            if (instance.ParentId == parent.Id && instance.Slot == slot)
            {
                return instance;
            }
        }

        return null;
    }

    public string? Equip(Guid id)
    {
        ItemInstance? item = Find(id);

        if (item == null || !item.IsLoose)
        {
            return "That is not in your bag.";
        }

        if (item.Type != ItemType.Phone)
        {
            return "That cannot be equipped.";
        }

        if (Equipped(SlotType.Device) != null)
        {
            return "You already have a device equipped. Unequip it first.";
        }

        item.Slot = SlotType.Device;
        return null;
    }

    public string? Unequip(Guid id)
    {
        ItemInstance? item = Find(id);

        if (item == null || item.ParentId != null || item.Slot == null)
        {
            return "That is not equipped.";
        }

        item.Slot = null;
        return null;
    }

    // At a workbench: the battery comes out of the phone and goes loose in the bag.
    public string? RemoveBattery(Guid phoneId)
    {
        ItemInstance? phone = Find(phoneId);
        ItemInstance? battery = phone == null ? null : Inside(phone, SlotType.Battery);

        if (battery == null)
        {
            return "That phone has no battery.";
        }

        battery.ParentId = null;
        battery.Slot = null;
        return null;
    }

    // At a workbench: the fullest battery the player has goes in, where one still in its
    // pack (a stack) counts as full. A battery from the stack gains its identity, with a
    // full charge, the moment it goes into something. A dead battery never goes in.
    public string? InsertBattery(Guid phoneId)
    {
        ItemInstance? phone = Find(phoneId);

        if (phone == null || phone.Type != ItemType.Phone)
        {
            return "That is not a phone you have.";
        }

        if (Inside(phone, SlotType.Battery) != null)
        {
            return "That phone already has a battery. Take it out first.";
        }

        ItemInstance? best = null;

        foreach (ItemInstance instance in Instances)
        {
            if (instance.IsLoose && instance.Type == ItemType.Battery && (best == null || instance.Charge > best.Charge))
            {
                best = instance;
            }
        }

        bool packed = Stacks.Count(ItemType.Battery, ItemTier.Standard) > 0;

        if (packed && (best == null || best.Charge < 1f))
        {
            Stacks.TryRemove(ItemType.Battery, ItemTier.Standard, 1);
            best = new ItemInstance(Guid.NewGuid(), ItemType.Battery, ItemTier.Standard) { Charge = 1f };
            Instances.Add(best);
        }
        else if (best == null || best.Charge == null || best.Charge <= 0f)
        {
            return "You have no charged battery. The electronics shop sells them.";
        }

        best.ParentId = phone.Id;
        best.Slot = SlotType.Battery;
        return null;
    }

    // The equipped phone's battery, or null with no phone or no battery in it.
    public ItemInstance? DeviceBattery()
    {
        ItemInstance? phone = Equipped(SlotType.Device);
        return phone == null ? null : Inside(phone, SlotType.Battery);
    }
}
