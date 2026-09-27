namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Town;

public class StrollTests
{
    [Fact]
    public void AStrollerWalksAtPaceAndWrapsRoundTheLoop()
    {
        Stroll stroll = new Stroll(20f, 19f, new Random(1));

        stroll.Advance(1);

        Assert.Equal((19f + Stroll.Pace) % 20f, stroll.Along, 3);
    }

    [Fact]
    public void AStrollerStoppedToTalkStandsThenGoesOn()
    {
        Stroll stroll = new Stroll(1000f, 0f, new Random(3));

        stroll.Stop(4);
        stroll.Advance(3);

        Assert.False(stroll.Walking);
        Assert.Equal(0f, stroll.Along);

        stroll.Advance(1.1);
        stroll.Advance(1);

        Assert.True(stroll.Along > 0f);
    }

    [Fact]
    public void AStrollerStopsWithinTheLongestWalkAndGoesOnAfter()
    {
        Stroll stroll = new Stroll(1000f, 0f, new Random(2));
        double walked = 0;

        while (stroll.Walking && walked < 100)
        {
            stroll.Advance(0.1);
            walked += 0.1;
        }

        Assert.False(stroll.Walking);
        Assert.InRange(stroll.Along, Stroll.MinWalk - 0.5f, Stroll.MaxWalk + 0.5f);

        stroll.Advance(Stroll.MaxPause + 0.1);
        float before = stroll.Along;
        stroll.Advance(1);

        Assert.True(stroll.Along > before);
    }
}
