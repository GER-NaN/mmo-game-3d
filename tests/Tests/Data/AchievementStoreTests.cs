namespace MmoGame3d.Tests.Data;

using MmoGame3d.Data.Accounts;
using MmoGame3d.Data.Players;
using MmoGame3d.Data.Progress;

[Collection(DatabaseCollection.Name)]
public class AchievementStoreTests
{
    private readonly TestDatabase _database;

    public AchievementStoreTests(TestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public void AnAchievementIsKeptOnceAndLoadsBack()
    {
        Guid accountId = new AccountStore(_database.Database).GetOrCreate(Guid.NewGuid());
        Guid player = new PlayerStore(_database.Database).GetOrCreate(
            new PlayerRecord { PlayerId = Guid.NewGuid(), AccountId = accountId, DisplayName = "Ada", Zone = "town" }, out _).PlayerId;
        AchievementStore store = new AchievementStore(_database.Database);

        store.Grant(player, "crack-a-code");
        store.Grant(player, "crack-a-code");
        store.Grant(player, "friend");

        Assert.Equal(new HashSet<string> { "crack-a-code", "friend" }, store.Load(player));
    }
}
