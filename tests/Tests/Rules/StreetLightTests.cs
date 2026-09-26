namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Town;

public class StreetLightTests
{
    private static readonly DateTime Noon = new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void TheRepairNeedsTheJobAndAPart()
    {
        StreetLights lights = StreetLights.Broken();
        Inventory bag = new Inventory();

        Assert.NotNull(lights.CannotRepair(false, bag));
        Assert.NotNull(lights.CannotRepair(true, bag));

        bag.Add(ItemType.RamStick, ItemTier.Enhanced, 1);

        Assert.Null(lights.CannotRepair(true, bag));
    }

    [Fact]
    public void ARepairUsesOnePartAndLightsTheStreet()
    {
        StreetLights lights = StreetLights.Broken();
        Inventory bag = new Inventory();
        bag.Add(ItemType.RamStick, ItemTier.Standard, 2);

        lights.Repair(bag, "Alice", Noon);

        Assert.True(lights.Working);
        Assert.Equal("Alice", lights.RepairedBy);
        Assert.Equal(1, bag.Count(ItemType.RamStick, ItemTier.Standard));
        Assert.NotNull(lights.CannotRepair(true, bag));
    }

    [Fact]
    public void TheLightsGoDarkAgainOnlyWhenTheRepairHasHeldItsTime()
    {
        StreetLights lights = StreetLights.Broken();
        Inventory bag = new Inventory();
        bag.Add(ItemType.RamStick, ItemTier.Standard, 1);
        lights.Repair(bag, "Alice", Noon);

        Assert.False(lights.BreakIfDue(Noon.AddHours(1)));
        Assert.True(lights.BreakIfDue(Noon + StreetLights.HoldsFor));
        Assert.False(lights.Working);
    }
}
