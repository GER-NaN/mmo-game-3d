namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Items;

public class EquipmentTests
{
    [Fact]
    public void TheStarterPhoneIsLooseInTheBagAtTenPercent()
    {
        Belongings mine = new Belongings(new Inventory(), Belongings.StarterKit());
        ItemInstance phone = mine.Instances.Single(item => item.Type == ItemType.Phone);

        Assert.True(phone.IsLoose);
        Assert.Equal(10, Power.Percent(mine.Inside(phone, SlotType.Battery)!.Charge));
    }

    [Fact]
    public void AnEquippedPhoneWithChargeCanGoOnline()
    {
        Belongings mine = new Belongings(new Inventory(), Belongings.StarterKit());
        ItemInstance phone = mine.Instances.Single(item => item.Type == ItemType.Phone);

        Assert.NotNull(Power.CannotGoOnline(mine));
        Assert.Null(mine.Equip(phone.Id));
        Assert.Null(Power.CannotGoOnline(mine));
    }

    [Fact]
    public void OneDeviceAtATime()
    {
        List<ItemInstance> two = Belongings.StarterKit();
        two.AddRange(Belongings.StarterKit());
        Belongings mine = new Belongings(new Inventory(), two);
        List<ItemInstance> phones = mine.Instances.Where(item => item.Type == ItemType.Phone).ToList();

        Assert.Null(mine.Equip(phones[0].Id));
        Assert.NotNull(mine.Equip(phones[1].Id));
    }

    [Fact]
    public void ADeadBatteryIsSwappedForABoughtOneAtFullCharge()
    {
        Inventory stacks = new Inventory();
        stacks.Add(ItemType.Battery, ItemTier.Standard, 1);
        Belongings mine = new Belongings(stacks, Belongings.StarterKit());
        ItemInstance phone = mine.Instances.Single(item => item.Type == ItemType.Phone);
        mine.Inside(phone, SlotType.Battery)!.Charge = 0f;
        mine.Equip(phone.Id);

        Assert.NotNull(Power.CannotGoOnline(mine));
        Assert.Null(mine.RemoveBattery(phone.Id));

        // The dead one is loose now; the bought one is fuller, so it goes in.
        Assert.Null(mine.InsertBattery(phone.Id));
        Assert.Equal(1f, mine.DeviceBattery()!.Charge);
        Assert.Equal(0, stacks.Count(ItemType.Battery, ItemTier.Standard));
        Assert.Null(Power.CannotGoOnline(mine));
    }

    [Fact]
    public void WithNoBatteryAnywhereNothingGoesIn()
    {
        Belongings mine = new Belongings(new Inventory(), Belongings.StarterKit());
        ItemInstance phone = mine.Instances.Single(item => item.Type == ItemType.Phone);
        ItemInstance battery = mine.Inside(phone, SlotType.Battery)!;
        mine.Instances.Remove(battery);

        Assert.NotNull(mine.InsertBattery(phone.Id));
    }

    [Fact]
    public void APhoneAndItsBatteryCrossTheWireIntact()
    {
        List<ItemInstance> sent = Belongings.StarterKit();

        string[] ids;
        int[] meta;
        float[] charges;
        InstanceWire.Pack(sent, out ids, out meta, out charges);
        List<ItemInstance> received = InstanceWire.Unpack(ids, meta, charges);

        Belongings mine = new Belongings(new Inventory(), received);
        ItemInstance phone = received.Single(item => item.Type == ItemType.Phone);

        Assert.Null(phone.Charge);
        Assert.Equal(Belongings.StarterCharge, mine.Inside(phone, SlotType.Battery)!.Charge);
    }

    [Fact]
    public void OnlineDrainsFasterThanCarried()
    {
        float carried = Power.Drain(1f, 3600f, false);
        float online = Power.Drain(1f, 3600f, true);

        Assert.True(online < carried);
        Assert.Equal(0f, Power.Drain(0.01f, 3600f, true));
    }
}
