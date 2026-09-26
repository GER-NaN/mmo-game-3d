namespace MmoGame3d.Rules.Items;

/// <summary>
/// What a player carries, as stacks: a count per type and tier. There is no capacity
/// yet; the design gives capacity to a backpack you buy.
/// </summary>
public class Inventory
{
    private readonly List<ItemStack> _stacks = new List<ItemStack>();

    public IReadOnlyList<ItemStack> Stacks
    {
        get { return _stacks; }
    }

    public void Add(ItemType type, ItemTier tier, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Add a positive quantity.");
        }

        ItemStack? stack = Find(type, tier);

        if (stack == null)
        {
            _stacks.Add(new ItemStack(type, tier, quantity));
        }
        else
        {
            stack.Quantity += quantity;
        }
    }

    // All or nothing: false, and nothing taken, when there is not enough.
    public bool TryRemove(ItemType type, ItemTier tier, int quantity)
    {
        ItemStack? stack = Find(type, tier);

        if (quantity <= 0 || stack == null || stack.Quantity < quantity)
        {
            return false;
        }

        stack.Quantity -= quantity;

        if (stack.Quantity == 0)
        {
            _stacks.Remove(stack);
        }

        return true;
    }

    public int Count(ItemType type, ItemTier tier)
    {
        ItemStack? stack = Find(type, tier);
        return stack == null ? 0 : stack.Quantity;
    }

    private ItemStack? Find(ItemType type, ItemTier tier)
    {
        foreach (ItemStack stack in _stacks)
        {
            if (stack.Type == type && stack.Tier == tier)
            {
                return stack;
            }
        }

        return null;
    }
}

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
