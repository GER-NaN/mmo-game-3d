namespace MmoGame3d.Tests.Data;

using MmoGame3d.Data.Accounts;
using MmoGame3d.Data.Players;
using MmoGame3d.Data.Social;
using MmoGame3d.Rules.Social;

[Collection(DatabaseCollection.Name)]
public class WhoisStoreTests
{
    private readonly TestDatabase _database;

    public WhoisStoreTests(TestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public void APageKeepsItsPlanAndSettings()
    {
        Guid owner = NewPlayer("Planner");
        WhoisStore store = new WhoisStore(_database.Database);

        Assert.True(store.LoadSettings(owner).ShowLocation);

        store.SaveSettings(owner, new WhoisSettings { Plan = "Fixing the grid", ShowSkills = true, ShowLocation = false });
        WhoisSettings loaded = store.LoadSettings(owner);

        Assert.Equal("Fixing the grid", loaded.Plan);
        Assert.True(loaded.ShowSkills);
        Assert.False(loaded.ShowLocation);
    }

    [Fact]
    public void PropsAreOnePerVisitorAndCanBeTakenBack()
    {
        Guid owner = NewPlayer("Popular");
        Guid visitor = NewPlayer("Fan");
        WhoisStore store = new WhoisStore(_database.Database);

        Assert.True(store.ToggleProps(visitor, owner));
        Assert.Equal(1, store.LoadProfile(owner, visitor)!.Props);
        Assert.True(store.LoadProfile(owner, visitor)!.GavePropsToo);

        Assert.False(store.ToggleProps(visitor, owner));
        Assert.Equal(0, store.LoadProfile(owner, visitor)!.Props);
    }

    [Fact]
    public void SearchFindsPlayersByPartOfTheirName()
    {
        string unique = "Zq" + Guid.NewGuid().ToString("N").Substring(0, 6);
        NewPlayer(unique + "One");
        NewPlayer(unique + "Two");

        Assert.Equal(2, new WhoisStore(_database.Database).Search(unique.ToLowerInvariant()).Count);
    }

    private Guid NewPlayer(string name)
    {
        Guid accountId = new AccountStore(_database.Database).GetOrCreate(Guid.NewGuid());
        return new PlayerStore(_database.Database).GetOrCreate(
            new PlayerRecord { PlayerId = Guid.NewGuid(), AccountId = accountId, DisplayName = name, Zone = "town" }, out _).PlayerId;
    }
}
