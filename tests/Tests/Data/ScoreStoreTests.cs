namespace MmoGame3d.Tests.Data;

using MmoGame3d.Data.Accounts;
using MmoGame3d.Data.Players;
using MmoGame3d.Data.Scores;

[Collection(DatabaseCollection.Name)]
public class ScoreStoreTests
{
    private readonly TestDatabase _database;

    public ScoreStoreTests(TestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public void TheBoardTakesEachPlayersBestAndBreaksTiesOnTime()
    {
        // A board of its own, so earlier runs' scores are not on it.
        string objective = "test-" + Guid.NewGuid();
        ScoreStore store = new ScoreStore(_database.Database);
        Guid ada = NewPlayer("Ada");
        Guid bo = NewPlayer("Bo");

        store.Add(objective, ada, "Ada", 6, 50);
        store.Add(objective, ada, "Ada", 4, 90);
        store.Add(objective, bo, "Bo", 4, 30);

        List<ScoreRecord> top = store.Top(objective, true, 10);

        Assert.Equal(new[] { "Bo", "Ada" }, top.Select(s => s.PlayerName));
        Assert.Equal(4, top[1].Score);
        Assert.Equal(4, store.Best(objective, ada, true)!.Score);
        Assert.Null(store.Best(objective, NewPlayer("Cy"), true));
    }

    private Guid NewPlayer(string name)
    {
        Guid accountId = new AccountStore(_database.Database).GetOrCreate(Guid.NewGuid());
        return new PlayerStore(_database.Database).GetOrCreate(
            new PlayerRecord { PlayerId = Guid.NewGuid(), AccountId = accountId, DisplayName = name, Zone = "town" }, out _).PlayerId;
    }
}
