namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Players;

public class LooksTests
{
    [Fact]
    public void AnUnknownLookFallsBackToTheDefault()
    {
        Assert.Equal(Looks.Default, Looks.OrDefault("dragon"));
        Assert.Equal("b", Looks.OrDefault("b"));
    }
}
