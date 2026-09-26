namespace MmoGame3d.Tests.Data;

using MmoGame3d.Data;
using MmoGame3d.Data.Accounts;
using MmoGame3d.Data.Players;

[Collection(DatabaseCollection.Name)]
public class PlayerStoreTests
{
    private readonly Database _database;
    private readonly AccountStore _accounts;
    private readonly PlayerStore _players;

    public PlayerStoreTests(TestDatabase database)
    {
        _database = database.Database;
        _accounts = new AccountStore(_database);
        _players = new PlayerStore(_database);
    }

    [Fact]
    public void TheSameLicenseKeyIsTheSameAccount()
    {
        Guid key = Guid.NewGuid();

        Assert.Equal(_accounts.GetOrCreate(key), _accounts.GetOrCreate(key));
        Assert.NotEqual(_accounts.GetOrCreate(key), _accounts.GetOrCreate(Guid.NewGuid()));
    }

    [Fact]
    public void AReturningPlayerKeepsTheirNameAndSavedPosition()
    {
        Guid accountId = _accounts.GetOrCreate(Guid.NewGuid());
        PlayerRecord first = _players.GetOrCreate(NewPlayer(accountId, "First"), out bool created);
        Assert.True(created);

        first.PositionX = 7f;
        _players.Save(first);

        PlayerRecord again = _players.GetOrCreate(NewPlayer(accountId, "Renamed"), out bool createdAgain);

        Assert.False(createdAgain);
        Assert.Equal(first.PlayerId, again.PlayerId);
        Assert.Equal("First", again.DisplayName);
        Assert.Equal(7f, again.PositionX);
    }

    [Fact]
    public void MigrationsRunOnceOnly()
    {
        Assert.Empty(MigrationRunner.Run(_database));
    }

    private static PlayerRecord NewPlayer(Guid accountId, string name)
    {
        return new PlayerRecord { PlayerId = Guid.NewGuid(), AccountId = accountId, DisplayName = name, Zone = "town" };
    }
}
