namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Items;

public class InventoryWireTests
{
    [Fact]
    public void AStackComesBackAsItWent()
    {
        List<ItemStack> sent = new List<ItemStack>
        {
            new ItemStack(ItemType.GpuCore, ItemTier.Elite, 3),
            new ItemStack(ItemType.RamStick, ItemTier.Standard, 12),
        };

        List<ItemStack> received = InventoryWire.Unpack(InventoryWire.Pack(sent));

        Assert.Equal(2, received.Count);
        Assert.Equal(ItemType.RamStick, received[1].Type);
        Assert.Equal(12, received[1].Quantity);
    }

    [Fact]
    public void AnUnknownTypeIsSkipped()
    {
        List<ItemStack> received = InventoryWire.Unpack(new[] { 999, 0, 1, (int)ItemType.GpuCore, 0, 2 });

        ItemStack only = Assert.Single(received);
        Assert.Equal(ItemType.GpuCore, only.Type);
    }
}
