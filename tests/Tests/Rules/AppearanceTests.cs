namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Players;

public class AppearanceTests
{
    [Fact]
    public void AnAppearanceComesBackFromItsString()
    {
        Appearance appearance = new Appearance { Base = "b", Skin = 3, Hair = 5, Top = 2, Bottom = 7, Backpack = false, Glasses = true };

        Appearance read = Appearance.Parse(appearance.Format());

        Assert.Equal(appearance.Format(), read.Format());
        Assert.False(read.Backpack);
    }

    [Fact]
    public void AnOldLookIdStillReads()
    {
        Appearance read = Appearance.Parse("b");

        Assert.Equal("b", read.Base);
        Assert.Equal(0, read.Hair);
        Assert.True(read.Backpack);
    }

    [Fact]
    public void NonsenseBecomesSomethingValid()
    {
        Assert.Equal(new Appearance().Format(), Appearance.Normalize("zz|99|-4|x"));
    }
}
