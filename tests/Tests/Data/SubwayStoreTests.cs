namespace MmoGame3d.Tests.Data;

using MmoGame3d.Data.Accounts;
using MmoGame3d.Data.Players;
using MmoGame3d.Data.Town;
using MmoGame3d.Rules.Town;

[Collection(DatabaseCollection.Name)]
public class SubwayStoreTests
{
    private readonly TestDatabase _database;

    public SubwayStoreTests(TestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public void ANameIsSprayedOnceAndStaysAsItWas()
    {
        // A wall of its own, so earlier runs' tags are not on it.
        string wall = "test-" + Guid.NewGuid();
        SubwayStore store = new SubwayStore(_database.Database);
        Guid ada = NewPlayer("Ada");

        bool made;
        SubwayTag first = store.Spray(wall, ada, "Ada", SubwayWall.Paints[1], out made);
        Assert.True(made);

        SubwayTag again = store.Spray(wall, ada, "Ada", SubwayWall.Paints[2], out made);
        Assert.False(made);
        Assert.Equal(first.Id, again.Id);
        Assert.Equal(SubwayWall.Paints[1], again.Paint);
        Assert.Equal(1, store.Count(wall));
    }

    [Fact]
    public void TheWallShowsTheNewestAndTheBookHasThemAllInOrder()
    {
        string wall = "test-" + Guid.NewGuid();
        SubwayStore store = new SubwayStore(_database.Database);
        bool made;

        foreach (string name in new[] { "One", "Two", "Three" })
        {
            store.Spray(wall, NewPlayer(name), name, SubwayWall.Paints[0], out made);
        }

        Assert.Equal(new[] { "Two", "Three" }, store.Newest(wall, 2).Select(t => t.Name));
        Assert.Equal(new[] { "Two", "Three" }, store.Page(wall, 1, 5).Select(t => t.Name));
    }

    private Guid NewPlayer(string name)
    {
        Guid accountId = new AccountStore(_database.Database).GetOrCreate(Guid.NewGuid());
        return new PlayerStore(_database.Database).GetOrCreate(
            new PlayerRecord { PlayerId = Guid.NewGuid(), AccountId = accountId, DisplayName = name, Zone = "town" }, out _).PlayerId;
    }
}
