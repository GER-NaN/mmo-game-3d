namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Items;

public class HandoverTests
{
    [Fact]
    public void GivingMovesTheStackFromOneBagToTheOther()
    {
        Inventory mine = new Inventory();
        Inventory theirs = new Inventory();
        mine.Add(ItemType.RamStick, ItemTier.Enhanced, 3);

        Assert.Null(Handover.Give(mine, theirs, ItemType.RamStick, ItemTier.Enhanced, 2));

        Assert.Equal(1, mine.Count(ItemType.RamStick, ItemTier.Enhanced));
        Assert.Equal(2, theirs.Count(ItemType.RamStick, ItemTier.Enhanced));
    }

    [Fact]
    public void GivingMoreThanHeldGivesNothing()
    {
        Inventory mine = new Inventory();
        Inventory theirs = new Inventory();
        mine.Add(ItemType.GpuCore, ItemTier.Standard, 1);

        Assert.NotNull(Handover.Give(mine, theirs, ItemType.GpuCore, ItemTier.Standard, 2));
        Assert.Equal(1, mine.Count(ItemType.GpuCore, ItemTier.Standard));
        Assert.Empty(theirs.Stacks);
    }

    [Fact]
    public void DollarsMoveOnlyWhenThereAreEnough()
    {
        int from;
        int to;

        Assert.Null(Handover.GiveDollars(10, 0, 4, out from, out to));
        Assert.Equal(6, from);
        Assert.Equal(4, to);

        Assert.NotNull(Handover.GiveDollars(3, 0, 4, out from, out to));
        Assert.Equal(3, from);
    }
}
