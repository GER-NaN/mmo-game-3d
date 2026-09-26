namespace MmoGame3d.Rules.Items;

/// <summary>
/// How a player's instances travel to their owner: three parallel arrays that Godot
/// sends packed. Ids as strings; four ints each (type, tier, slot, parent) where slot
/// and parent are -1 for none and a parent is its index in the same list; and one float
/// each for the charge, -1 for none.
/// </summary>
public static class InstanceWire
{
    private const int IntsPerInstance = 4;

    public static void Pack(IReadOnlyList<ItemInstance> instances, out string[] ids, out int[] meta, out float[] charges)
    {
        ids = new string[instances.Count];
        meta = new int[instances.Count * IntsPerInstance];
        charges = new float[instances.Count];

        for (int i = 0; i < instances.Count; i++)
        {
            ItemInstance instance = instances[i];
            ids[i] = instance.Id.ToString();
            meta[i * IntsPerInstance] = (int)instance.Type;
            meta[(i * IntsPerInstance) + 1] = (int)instance.Tier;
            meta[(i * IntsPerInstance) + 2] = instance.Slot == null ? -1 : (int)instance.Slot.Value;
            meta[(i * IntsPerInstance) + 3] = IndexOf(instances, instance.ParentId);
            charges[i] = instance.Charge ?? -1f;
        }
    }

    public static List<ItemInstance> Unpack(string[] ids, int[] meta, float[] charges)
    {
        List<ItemInstance> instances = new List<ItemInstance>();
        int count = Math.Min(ids.Length, Math.Min(meta.Length / IntsPerInstance, charges.Length));

        for (int i = 0; i < count; i++)
        {
            Guid id;

            if (!Guid.TryParse(ids[i], out id))
            {
                continue;
            }

            int slot = meta[(i * IntsPerInstance) + 2];
            instances.Add(new ItemInstance(id, (ItemType)meta[i * IntsPerInstance], (ItemTier)meta[(i * IntsPerInstance) + 1])
            {
                Slot = slot < 0 ? null : (SlotType)slot,
                Charge = charges[i] < 0f ? null : charges[i],
            });
        }

        // Parents are set once every instance exists, since a parent can come later in
        // the list than its child.
        for (int i = 0; i < instances.Count && i < count; i++)
        {
            int parent = meta[(i * IntsPerInstance) + 3];

            if (parent >= 0 && parent < instances.Count)
            {
                instances[i].ParentId = instances[parent].Id;
            }
        }

        return instances;
    }

    private static int IndexOf(IReadOnlyList<ItemInstance> instances, Guid? id)
    {
        if (id == null)
        {
            return -1;
        }

        for (int i = 0; i < instances.Count; i++)
        {
            if (instances[i].Id == id.Value)
            {
                return i;
            }
        }

        return -1;
    }
}
