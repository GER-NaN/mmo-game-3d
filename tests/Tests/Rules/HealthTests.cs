namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Players;

public class HealthTests
{
    [Fact]
    public void HpStaysBetweenZeroAndAHundred()
    {
        Assert.Equal(0, Health.Hurt(10, 25));
        Assert.Equal(Health.Max, Health.Heal(95, 20));
    }

    [Fact]
    public void BelowFiftyYouAreSlowedAndAtZeroYouFaint()
    {
        Assert.False(Health.IsSlowed(50));
        Assert.True(Health.IsSlowed(49));
        Assert.True(Health.HasFainted(Health.Hurt(5, 5)));
    }
}
