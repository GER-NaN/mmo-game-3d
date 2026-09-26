namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Terminals;

public class TerminalTests
{
    private readonly Guid _alice = Guid.NewGuid();
    private readonly Guid _bob = Guid.NewGuid();

    [Fact]
    public void OnePlayerPerTerminal()
    {
        TerminalAccess access = new TerminalAccess();

        Assert.Null(access.Use(_alice, "town/library", true));
        Assert.NotNull(access.Use(_bob, "town/library", true));
    }

    [Fact]
    public void OneTerminalPerPlayer()
    {
        TerminalAccess access = new TerminalAccess();
        access.Use(_alice, "town/library", true);

        Assert.NotNull(access.Use(_alice, "town/kiosk", true));
    }

    [Fact]
    public void ADisabledTerminalCannotBeUsed()
    {
        Assert.NotNull(new TerminalAccess().Use(_alice, "town/library", false));
    }

    [Fact]
    public void LeavingFreesTheTerminal()
    {
        TerminalAccess access = new TerminalAccess();
        access.Use(_alice, "town/library", true);

        Assert.Equal("town/library", access.Leave(_alice));
        Assert.Null(access.Use(_bob, "town/library", true));
    }

    [Fact]
    public void APhoneOffersFewerAppsAndStillShowsLockedOnes()
    {
        List<TerminalApp> phone = TerminalApps.For(TerminalType.Phone);
        List<TerminalApp> desk = TerminalApps.For(TerminalType.Public);

        Assert.True(phone.Count < desk.Count);
        Assert.Contains(phone, app => app.State == AppState.Locked);
    }
}
