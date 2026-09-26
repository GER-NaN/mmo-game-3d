namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Items;

public class RecyclingTests
{
    [Fact]
    public void OneOfAStackGoesInAndPaysItsTierValue()
    {
        Inventory stacks = new Inventory();
        stacks.Add(ItemType.GpuCore, ItemTier.Enhanced, 2);

        Assert.Null(Recycling.RecycleOne(stacks, ItemType.GpuCore, ItemTier.Enhanced, out int dollars));
        Assert.Equal(Recycling.ValueOf(ItemType.GpuCore, ItemTier.Standard) * 2, dollars);
        Assert.Equal(1, stacks.Count(ItemType.GpuCore, ItemTier.Enhanced));
        Assert.NotNull(Recycling.RecycleOne(stacks, ItemType.RamStick, ItemTier.Standard, out _));
    }

    [Fact]
    public void APhoneGoesWithItsBatteryButNotWhileWorn()
    {
        List<ItemInstance> instances = Belongings.StarterKit();
        Belongings mine = new Belongings(new Inventory(), instances);
        ItemInstance phone = instances[0];

        mine.Equip(phone.Id);
        Assert.NotNull(Recycling.RecycleThing(mine, phone.Id, out _));

        mine.Unequip(phone.Id);
        Assert.Null(Recycling.RecycleThing(mine, phone.Id, out int dollars));
        Assert.Empty(instances);
        Assert.Equal(Recycling.ValueOf(ItemType.Phone, ItemTier.Standard) + Recycling.ValueOf(ItemType.Battery, ItemTier.Standard), dollars);
    }
}
