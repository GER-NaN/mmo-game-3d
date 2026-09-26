namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Social;

public class WhoisTests
{
    [Fact]
    public void LocationShowsInTownAndGoesDarkAwayFromItOrWhenTurnedOff()
    {
        Assert.Equal("In town", Whois.Where("town", true));
        Assert.Equal("At the college", Whois.Where("college", true));
        Assert.Equal("", Whois.Where("outskirts", true));
        Assert.Equal("", Whois.Where("taxi-3", true));
        Assert.Equal("", Whois.Where("town", false));
    }

    [Fact]
    public void LastSeenReadsInTheLargestUnit()
    {
        DateTime now = new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);

        Assert.Equal("Offline, last seen 5 minutes ago", Whois.LastSeen(now.AddMinutes(-5), now));
        Assert.Equal("Offline, last seen 1 hour ago", Whois.LastSeen(now.AddHours(-1.5), now));
        Assert.Equal("Offline, last seen 2 days ago", Whois.LastSeen(now.AddDays(-2.2), now));
    }
}
