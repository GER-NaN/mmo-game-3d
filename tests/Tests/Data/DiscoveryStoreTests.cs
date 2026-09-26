namespace MmoGame3d.Tests.Data;

using MmoGame3d.Data.Accounts;
using MmoGame3d.Data.Maps;
using MmoGame3d.Data.Players;

[Collection(DatabaseCollection.Name)]
public class DiscoveryStoreTests
{
    private readonly TestDatabase _database;

    public DiscoveryStoreTests(TestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public void AZonesCellsAreSavedAndReplaced()
    {
        Guid accountId = new AccountStore(_database.Database).GetOrCreate(Guid.NewGuid());
        PlayerRecord player = new PlayerStore(_database.Database).GetOrCreate(
            new PlayerRecord { PlayerId = Guid.NewGuid(), AccountId = accountId, DisplayName = "Mapper", Zone = "town" }, out _);
        DiscoveryStore store = new DiscoveryStore(_database.Database);

        store.Save(player.PlayerId, "town", new byte[] { 1, 2 });
        store.Save(player.PlayerId, "town", new byte[] { 3 });

        Dictionary<string, byte[]> loaded = store.Load(player.PlayerId);

        Assert.Equal(new byte[] { 3 }, loaded["town"]);
    }
}
