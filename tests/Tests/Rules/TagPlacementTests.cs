namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Town;

public class TagPlacementTests
{
    [Fact]
    public void OnABareWallATagGoesWhereWanted()
    {
        TagPlace? place = TagPlacement.Find(new List<SubwayTag>(), "Ada", 70, 5f, 1f, 1.6f);

        Assert.NotNull(place);
        Assert.InRange(place!.X, 0.95f, 1.05f);
        Assert.InRange(place.Y, 1.55f, 1.65f);
        Assert.Equal(5f, place.Angle);
        Assert.Equal(70, place.Size);
    }

    [Fact]
    public void ATagTakenSpotSendsTheNextToTheNearestFreeOne()
    {
        SubwayTag cora = Placed("Cora", 0f, 1.6f, 70, 8f);

        TagPlace? place = TagPlacement.Find(new[] { cora }, "Jim Bob", 70, -6f, 0f, 1.6f);

        Assert.NotNull(place);
        Assert.False(Overlap(cora, "Jim Bob", place!));

        // Nearest: just clear of Cora, not somewhere far along the wall.
        float cleared = (TagPlacement.Width("Cora", 70) + TagPlacement.Width("Jim Bob", 70)) / 2f;
        float away = MathF.Sqrt((place.X * place.X) + ((place.Y - 1.6f) * (place.Y - 1.6f)));
        Assert.True(away < cleared + 0.5f, "placed " + away + " m away");
    }

    [Fact]
    public void AFullWallHasNoRoom()
    {
        List<SubwayTag> wall = new List<SubwayTag>();

        for (int i = 0; i < 400; i++)
        {
            TagPlace? place = TagPlacement.Find(wall, "Tagger" + i, 90, 0f, 0f, 1.6f);

            if (place == null)
            {
                return;
            }

            wall.Add(new SubwayTag { Id = i, Name = "Tagger" + i, Place = place });
        }

        Assert.Fail("the wall never filled");
    }

    [Fact]
    public void AWantedSpotOffTheWallLandsOnIt()
    {
        TagPlace? place = TagPlacement.Find(new List<SubwayTag>(), "Ada", 70, 0f, 50f, 9f);

        Assert.NotNull(place);
        Assert.True(place!.X + (TagPlacement.Width("Ada", 70) / 2f) <= SubwayWall.Width / 2f);
        Assert.True(place.Y + (TagPlacement.Height(70) / 2f) <= SubwayWall.High);
    }

    private static SubwayTag Placed(string name, float x, float y, int size, float angle)
    {
        return new SubwayTag { Id = 1, Name = name, Place = new TagPlace { X = x, Y = y, Size = size, Angle = angle } };
    }

    // The two unslanted boxes cross: a strict check, since slant only makes boxes bigger.
    private static bool Overlap(SubwayTag a, string name, TagPlace b)
    {
        float halfWidths = (TagPlacement.Width(a.Name, a.Place!.Size) + TagPlacement.Width(name, b.Size)) / 2f;
        float halfHeights = (TagPlacement.Height(a.Place.Size) + TagPlacement.Height(b.Size)) / 2f;
        return Math.Abs(a.Place.X - b.X) < halfWidths && Math.Abs(a.Place.Y - b.Y) < halfHeights;
    }
}
