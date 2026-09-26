namespace MmoGame3d.Rules.Items;

/// <summary>
/// How a bag travels from the server to its owner: three ints per stack (type, tier,
/// quantity) in one flat array, which Godot sends as a packed array without per-item
/// overhead. Only the owner gets it; nobody else learns what you carry.
/// </summary>
public static class InventoryWire
{
    private const int IntsPerStack = 3;

    public static int[] Pack(IReadOnlyList<ItemStack> stacks)
    {
        int[] packed = new int[stacks.Count * IntsPerStack];

        for (int i = 0; i < stacks.Count; i++)
        {
            packed[i * IntsPerStack] = (int)stacks[i].Type;
            packed[(i * IntsPerStack) + 1] = (int)stacks[i].Tier;
            packed[(i * IntsPerStack) + 2] = stacks[i].Quantity;
        }

        return packed;
    }

    // A malformed array or an unknown type yields what could be read, not an exception:
    // the client shows what it understands and the next snapshot corrects it.
    public static List<ItemStack> Unpack(int[] packed)
    {
        List<ItemStack> stacks = new List<ItemStack>();

        for (int i = 0; i + IntsPerStack - 1 < packed.Length; i += IntsPerStack)
        {
            ItemType type = (ItemType)packed[i];
            ItemTier tier = (ItemTier)packed[i + 1];
            int quantity = packed[i + 2];

            if (Enum.IsDefined(type) && Enum.IsDefined(tier) && quantity > 0)
            {
                stacks.Add(new ItemStack(type, tier, quantity));
            }
        }

        return stacks;
    }
}
