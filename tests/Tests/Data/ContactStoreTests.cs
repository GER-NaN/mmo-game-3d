namespace MmoGame3d.Tests.Data;

using MmoGame3d.Data.Accounts;
using MmoGame3d.Data.Players;
using MmoGame3d.Data.Social;
using MmoGame3d.Rules.Social;

[Collection(DatabaseCollection.Name)]
public class ContactStoreTests
{
    private readonly TestDatabase _database;

    public ContactStoreTests(TestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public void FriendsAndIgnoresComeBackWithTheirNames()
    {
        Guid me = NewPlayer("Me");
        Guid friend = NewPlayer("Pal");
        Guid pest = NewPlayer("Pest");
        ContactStore store = new ContactStore(_database.Database);

        store.Set(me, friend, false);
        store.Set(me, pest, false);
        store.Set(me, pest, true);

        Contacts contacts = store.Load(me);

        Assert.Equal("Pal", contacts.Friends[friend]);
        Assert.True(contacts.Ignores(pest));
        Assert.False(contacts.IsFriend(pest));

        store.Remove(me, friend);

        Assert.False(store.Load(me).IsFriend(friend));
    }

    private Guid NewPlayer(string name)
    {
        Guid accountId = new AccountStore(_database.Database).GetOrCreate(Guid.NewGuid());
        PlayerRecord player = new PlayerStore(_database.Database).GetOrCreate(
            new PlayerRecord { PlayerId = Guid.NewGuid(), AccountId = accountId, DisplayName = name, Zone = "town" }, out _);
        return player.PlayerId;
    }
}
