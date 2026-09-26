namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Items;

public class LootTableTests
{
    [Fact]
    public void AOneEntryTableAlwaysRollsThatEntryWithinItsRange()
    {
        LootTable table = new LootTable();
        table.Add(new LootEntry(ItemType.RamStick, ItemTier.Advanced, 5, 2, 4));
        Random random = new Random(7);

        for (int i = 0; i < 50; i++)
        {
            LootRoll roll = table.Roll(random);

            Assert.Equal(ItemType.RamStick, roll.Type);
            Assert.Equal(ItemTier.Advanced, roll.Tier);
            Assert.InRange(roll.Quantity, 2, 4);
        }
    }

    [Fact]
    public void AHeavierEntryComesUpMoreOften()
    {
        LootTable table = new LootTable();
        table.Add(new LootEntry(ItemType.GpuCore, ItemTier.Standard, 9, 1, 1));
        table.Add(new LootEntry(ItemType.GpuCore, ItemTier.Elite, 1, 1, 1));
        Random random = new Random(11);
        int elites = 0;

        for (int i = 0; i < 1000; i++)
        {
            if (table.Roll(random).Tier == ItemTier.Elite)
            {
                elites++;
            }
        }

        Assert.InRange(elites, 50, 150);
    }
}
