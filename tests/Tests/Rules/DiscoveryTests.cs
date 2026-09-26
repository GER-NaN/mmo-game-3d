namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Maps;

public class DiscoveryTests
{
    [Fact]
    public void StandingSomewhereDiscoversTheCellsInSightOnly()
    {
        Discovery town = new Discovery(100f, 100f);

        Assert.True(town.See(0f, 0f));

        Assert.True(town.IsDiscovered(4, 4));
        Assert.True(town.IsDiscovered(5, 5));
        Assert.False(town.IsDiscovered(0, 0));
        Assert.False(town.See(0f, 0f));
    }

    [Fact]
    public void DiscoveryComesBackFromItsBytes()
    {
        Discovery town = new Discovery(100f, 100f);
        town.See(-45f, 45f);

        Discovery loaded = new Discovery(100f, 100f);
        loaded.Load(town.ToBytes());

        Assert.Equal(town.DiscoveredCount(), loaded.DiscoveredCount());
        Assert.True(loaded.IsDiscovered(0, 9));
    }
}
