namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Town;

public class TaxiRootkitTests
{
    [Fact]
    public void TheTaxisStartInfectedAndOnlyTheOneWithTheJobCleansThem()
    {
        TaxiRootkit rootkit = TaxiRootkit.Infected();

        Assert.False(rootkit.Clean);
        Assert.NotNull(rootkit.CannotClean(false));
        Assert.Null(rootkit.CannotClean(true));

        rootkit.CleanOut("Ada", DateTime.UtcNow);

        Assert.True(rootkit.Clean);
        Assert.NotNull(rootkit.CannotClean(true));
    }

    [Fact]
    public void TheAiGetsBackInAfterAWhile()
    {
        DateTime cleaned = new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);
        TaxiRootkit rootkit = TaxiRootkit.Restore(true, cleaned, "Ada");

        Assert.False(rootkit.InfectIfDue(cleaned + TimeSpan.FromHours(1)));
        Assert.True(rootkit.InfectIfDue(cleaned + TaxiRootkit.HoldsFor));
        Assert.False(rootkit.Clean);
    }
}
