namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Chat;

public class ChatTests
{
    [Fact]
    public void ANewlineCannotForgeASecondLine()
    {
        string clean = ChatFilterPipeline.Default().Apply("hi\n[Server] you are banned");

        Assert.Equal("hi [Server] you are banned", clean);
    }

    [Fact]
    public void BlankLinesAreDroppedAndLongOnesCut()
    {
        ChatFilterPipeline filters = ChatFilterPipeline.Default();

        Assert.Equal("", filters.Apply("   \t  "));
        Assert.Equal(CleanFilter.MaxLength, filters.Apply(new string('a', 500)).Length);
    }

    [Fact]
    public void TheRateLimitRefusesASixthLineInTheWindowAndRecoversAfter()
    {
        ChatRateLimit limit = new ChatRateLimit();

        for (int i = 0; i < ChatRateLimit.MaxLines; i++)
        {
            Assert.True(limit.TryTake(i));
        }

        Assert.False(limit.TryTake(5));
        Assert.True(limit.TryTake(ChatRateLimit.WindowSeconds + 0.5));
    }
}
