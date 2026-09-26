namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.World;

public class ZoneIdTests
{
    [Fact]
    public void AnInstanceIsMadeFromItsScene()
    {
        string ride = ZoneIds.Instance(ZoneIds.Taxi, 3);

        Assert.Equal("taxi-3", ride);
        Assert.Equal(ZoneIds.Taxi, ZoneIds.SceneOf(ride));
    }

    [Fact]
    public void AnOrdinaryZoneIsItsOwnScene()
    {
        Assert.Equal(ZoneIds.Town, ZoneIds.SceneOf(ZoneIds.Town));
    }

    [Fact]
    public void TheFirstTownReadsOldTownAndARideReadsAsTheTaxi()
    {
        Assert.Equal("Old Town", ZoneIds.DisplayName(ZoneIds.Town));
        Assert.Equal("Robo taxi", ZoneIds.DisplayName(ZoneIds.Instance(ZoneIds.Taxi, 3)));
    }
}
