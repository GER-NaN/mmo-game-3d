namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Social;

public class GestureTests
{
    [Fact]
    public void AnEmoteCommandIsFoundInAChatLine()
    {
        Assert.Equal("wave", Gestures.EmoteIn(" /Wave "));
        Assert.Null(Gestures.EmoteIn("wave to me"));
    }

    [Fact]
    public void ActionsCannotBeAskedForAsEmotes()
    {
        Assert.Null(Gestures.EmoteIn("/" + Gestures.Repair));
        Assert.NotNull(Gestures.Find(Gestures.Repair));
    }
}
