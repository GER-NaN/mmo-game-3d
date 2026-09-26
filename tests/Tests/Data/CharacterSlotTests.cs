namespace MmoGame3d.Tests.Data;

using MmoGame3d.Data.Accounts;
using MmoGame3d.Data.Players;
using MmoGame3d.Rules.Players;

[Collection(DatabaseCollection.Name)]
public class CharacterSlotTests
{
    private readonly TestDatabase _database;

    public CharacterSlotTests(TestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public void AnAccountHoldsACharacterPerSlotAndLoadsOnlyItsOwn()
    {
        Guid account = new AccountStore(_database.Database).GetOrCreate(Guid.NewGuid());
        Guid stranger = new AccountStore(_database.Database).GetOrCreate(Guid.NewGuid());
        PlayerStore players = new PlayerStore(_database.Database);
        PlayerRecord first = New(account, "First");
        PlayerRecord second = New(account, "Second");

        Assert.True(players.Create(first, 0));
        Assert.True(players.Create(second, 1));
        Assert.False(players.Create(New(account, "Third"), 1));

        List<CharacterSummary> list = players.ListForAccount(account);
        Assert.Equal(new[] { "First", "Second" }, list.Select(c => c.DisplayName));
        Assert.Equal(-1, Characters.FreeSlot(list.Select(c => c.Slot)));

        Assert.NotNull(players.Load(second.PlayerId, account));
        Assert.Null(players.Load(second.PlayerId, stranger));
    }

    private static PlayerRecord New(Guid account, string name)
    {
        return new PlayerRecord { PlayerId = Guid.NewGuid(), AccountId = account, DisplayName = name, Zone = "town", Look = "a" };
    }
}
