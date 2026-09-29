namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Town;

public class SubwayWallTests
{
    [Fact]
    public void TagsCrossTheWireIntact()
    {
        List<SubwayTag> sent = new List<SubwayTag>
        {
            new SubwayTag { Id = 7, Name = "Cora", Paint = SubwayWall.Paints[0] },
            new SubwayTag { Id = 12, Name = "Jim Bob", Paint = SubwayWall.Paints[3] },
        };

        List<SubwayTag> received = SubwayWall.Unpack(SubwayWall.Pack(sent));

        Assert.Equal(new[] { 7L, 12L }, received.Select(t => t.Id));
        Assert.Equal("Jim Bob", received[1].Name);
        Assert.Equal(SubwayWall.Paints[3], received[1].Paint);
    }

    [Fact]
    public void APlaceCrossesTheWireWithItsTag()
    {
        List<SubwayTag> sent = new List<SubwayTag>
        {
            new SubwayTag { Id = 7, Name = "Cora", Paint = SubwayWall.Paints[0], Place = new TagPlace { X = -2.5f, Y = 1.3f, Angle = 8.5f, Size = 72 } },
        };

        TagPlace? place = SubwayWall.Unpack(SubwayWall.Pack(sent))[0].Place;

        Assert.NotNull(place);
        Assert.Equal(-2.5f, place!.X);
        Assert.Equal(8.5f, place.Angle);
        Assert.Equal(72, place.Size);
    }

    [Fact]
    public void AnEmptyWallHasNoTags()
    {
        Assert.Empty(SubwayWall.Unpack(""));
    }
}
