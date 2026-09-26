namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Players;

public class DisplayNameTests
{
    [Fact]
    public void AnOrdinaryNameIsFine()
    {
        Assert.Null(DisplayName.Problem("Gerald"));
    }

    [Fact]
    public void BlankOrTooLongNamesAreRefused()
    {
        Assert.NotNull(DisplayName.Problem("   "));
        Assert.NotNull(DisplayName.Problem(new string('a', DisplayName.MaxLength + 1)));
    }
}
