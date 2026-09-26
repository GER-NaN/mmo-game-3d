namespace MmoGame3d.Tests.Data;

using MmoGame3d.Data.Accounts;
using MmoGame3d.Data.Gardening;
using MmoGame3d.Data.Players;

[Collection(DatabaseCollection.Name)]
public class PlantStoreTests
{
    private readonly TestDatabase _database;

    public PlantStoreTests(TestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public void APlantKeepsItsCreatorDesignAndHistory()
    {
        Guid creator = NewPlayer("Gardener");
        PlantStore store = new PlantStore(_database.Database);

        long id = store.Create(creator, "Gardener", "Fern Gully", "pot_A_small;cactus_A,0,0,0,0,1", "Created by Gardener");
        store.AddEvent(id, "Placed outside the greenhouse");

        PlantRecord plant = store.Get(id)!;
        Assert.Equal("Gardener", plant.CreatorName);
        Assert.Equal(creator, plant.CreatedBy);
        Assert.Equal("Fern Gully", plant.Name);
        Assert.Equal(new[] { "Created by Gardener", "Placed outside the greenhouse" }, plant.History.Select(e => e.Text));
    }

    [Fact]
    public void TheNewestComeFirst()
    {
        Guid creator = NewPlayer("Grower");
        PlantStore store = new PlantStore(_database.Database);
        long older = store.Create(creator, "Grower", "", "pot_A_small;cactus_A,0,0,0,0,1", "made");
        long newer = store.Create(creator, "Grower", "", "pot_A_small;cactus_B,0,0,0,0,1", "made");

        List<PlantRecord> newest = store.Newest(2);

        Assert.Equal(new[] { newer, older }, newest.Select(p => p.Id));
    }

    private Guid NewPlayer(string name)
    {
        Guid accountId = new AccountStore(_database.Database).GetOrCreate(Guid.NewGuid());
        return new PlayerStore(_database.Database).GetOrCreate(
            new PlayerRecord { PlayerId = Guid.NewGuid(), AccountId = accountId, DisplayName = name, Zone = "town" }, out _).PlayerId;
    }
}
