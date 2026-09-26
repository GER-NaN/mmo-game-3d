namespace MmoGame3d.Tests.Data;

using MmoGame3d.Data.Town;

[Collection(DatabaseCollection.Name)]
public class TownStoreTests
{
    private readonly TownStore _town;

    public TownStoreTests(TestDatabase database)
    {
        _town = new TownStore(database.Database);
    }

    [Fact]
    public void WorldStateIsKeptAndReplaced()
    {
        string key = "test-" + Guid.NewGuid();

        Assert.Null(_town.Get(key));

        _town.Set(key, "one");
        _town.Set(key, "two");

        Assert.Equal("two", _town.Get(key));
    }

    [Fact]
    public void TheLogComesBackNewestFirst()
    {
        string zone = "test-" + Guid.NewGuid();
        _town.AddLog(zone, "first");
        _town.AddLog(zone, "second");

        List<TownLogEntry> log = _town.RecentLog(zone, 10);

        Assert.Equal(new[] { "second", "first" }, log.Select(entry => entry.Entry));
    }
}
