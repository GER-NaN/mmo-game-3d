namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Terminals;

public class StatusFeedTests
{
    [Fact]
    public void TheNewestIsFirstAndOnlyTheLastFewAreKept()
    {
        StatusFeed feed = new StatusFeed();

        for (int i = 0; i < StatusFeed.Kept + 3; i++)
        {
            feed.Post("18:0" + (i % 10), "event " + i);
        }

        Assert.Equal(StatusFeed.Kept, feed.Lines.Count);
        Assert.EndsWith("event " + (StatusFeed.Kept + 2), feed.Lines[0]);
        Assert.EndsWith("event 3", feed.Lines[feed.Lines.Count - 1]);
    }
}
