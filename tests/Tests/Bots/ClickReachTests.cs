namespace MmoGame3d.Tests.Bots;

using MmoGame3d.BotJudging;

// A bot clicks only what a person could click (BotBody.TryClick decides by ClickReach).
public class ClickReachTests
{
    private static readonly ScreenRect Window = new ScreenRect(0, 0, 1280, 720);

    [Fact]
    public void AButtonInTheWindowWithNoListIsClicked()
    {
        Assert.Equal(ClickMove.Click, ClickReach.Decide(Window, null, new ScreenRect(600, 300, 200, 30), 0));
    }

    [Fact]
    public void AButtonClippedByItsListIsScrolledToNotClicked()
    {
        // The registrar's second career, 2026-09-27: inside the window (y 631 of 720), but
        // below what the list shows (to y 600). A click there hit what lay under it.
        ScreenRect list = new ScreenRect(300, 100, 700, 500);

        Assert.Equal(ClickMove.WheelDown, ClickReach.Decide(Window, list, new ScreenRect(796, 631, 226, 31), 0));
    }

    [Fact]
    public void AButtonAboveWhatTheListShowsIsScrolledUpTo()
    {
        ScreenRect list = new ScreenRect(300, 100, 700, 500);

        Assert.Equal(ClickMove.WheelUp, ClickReach.Decide(Window, list, new ScreenRect(400, 40, 200, 30), 0));
    }

    [Fact]
    public void AButtonOffTheWindowWithNothingToScrollIsOffScreen()
    {
        // The workbench, 2026-09-27: a battery's button above the top of the window.
        Assert.Equal(ClickMove.OffScreen, ClickReach.Decide(new ScreenRect(0, 0, 1904, 992), null, new ScreenRect(151, -86, 255, 31), 0));
    }

    [Fact]
    public void AButtonTheWheelNeverBringsInIsOffScreen()
    {
        ScreenRect list = new ScreenRect(300, 100, 700, 500);

        Assert.Equal(ClickMove.OffScreen, ClickReach.Decide(Window, list, new ScreenRect(796, 900, 226, 31), ClickReach.MaxWheels));
    }

    [Fact]
    public void AButtonJustMadeWaitsForItsLayout()
    {
        // A rebuilt list's new button: no size and no place until the next layout, so a
        // click would land at the window's corner.
        Assert.Equal(ClickMove.Wait, ClickReach.Decide(Window, null, new ScreenRect(0, 0, 0, 0), 0));
    }

    [Fact]
    public void AFinishedPlantMustHaveBeenMade()
    {
        Assert.NotNull(PlantCheck.Judge(finished: true, cancelled: false, madeSeen: false, mostPieces: 3, piecesOffered: true, status: "Drag a piece"));
        Assert.Null(PlantCheck.Judge(finished: true, cancelled: false, madeSeen: true, mostPieces: 3, piecesOffered: true, status: ""));
    }

    [Fact]
    public void DragsThatNeverLandAreCaught()
    {
        Assert.NotNull(PlantCheck.Judge(finished: false, cancelled: false, madeSeen: false, mostPieces: 0, piecesOffered: true, status: "Off the soil"));
    }

    [Fact]
    public void ACancelledPlantIsNotJudged()
    {
        Assert.Null(PlantCheck.Judge(finished: false, cancelled: true, madeSeen: false, mostPieces: 0, piecesOffered: true, status: ""));
    }
}
