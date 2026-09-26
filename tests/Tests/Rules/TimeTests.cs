namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Time;

public class TimeTests
{
    [Fact]
    public void TheClockKeepsTheServersTimeZone()
    {
        WorldClock clock = new WorldClock("America/New_York", 0);

        // 17:00 UTC in July is 13:00 in New York (summer time).
        double seconds = clock.SecondsOfDay(new DateTime(2026, 7, 1, 17, 0, 0, DateTimeKind.Utc));

        Assert.Equal(13 * 3600, seconds, 1);
    }

    [Fact]
    public void TheOffsetShiftsTheHourAndWrapsPastMidnight()
    {
        WorldClock clock = new WorldClock("UTC", 3);

        double seconds = clock.SecondsOfDay(new DateTime(2026, 7, 1, 23, 0, 0, DateTimeKind.Utc));

        Assert.Equal(2 * 3600, seconds, 1);
    }

    [Fact]
    public void NoonIsBrightAndMidnightIsDark()
    {
        Assert.Equal(1.0, Daylight.SunStrength(12), 3);
        Assert.Equal(0.0, Daylight.SunStrength(0), 3);
        Assert.True(Daylight.Warmth(6.5) > Daylight.Warmth(12));
    }
}
