namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Items;

public class InventoryTests
{
    [Fact]
    public void TheSameTypeAndTierStackTogether()
    {
        Inventory inventory = new Inventory();

        inventory.Add(ItemType.GpuCore, ItemTier.Standard, 2);
        inventory.Add(ItemType.GpuCore, ItemTier.Standard, 3);
        inventory.Add(ItemType.GpuCore, ItemTier.Elite, 1);

        Assert.Equal(2, inventory.Stacks.Count);
        Assert.Equal(5, inventory.Count(ItemType.GpuCore, ItemTier.Standard));
    }

    [Fact]
    public void RemovingMoreThanHeldTakesNothing()
    {
        Inventory inventory = new Inventory();
        inventory.Add(ItemType.RamStick, ItemTier.Standard, 2);

        Assert.False(inventory.TryRemove(ItemType.RamStick, ItemTier.Standard, 3));
        Assert.Equal(2, inventory.Count(ItemType.RamStick, ItemTier.Standard));
    }

    [Fact]
    public void RemovingTheLastOneRemovesTheStack()
    {
        Inventory inventory = new Inventory();
        inventory.Add(ItemType.RamStick, ItemTier.Standard, 2);

        Assert.True(inventory.TryRemove(ItemType.RamStick, ItemTier.Standard, 2));
        Assert.Empty(inventory.Stacks);
    }
}
