namespace MmoGame3d.Tests.Data;

using MmoGame3d.Data.Accounts;
using MmoGame3d.Data.Events;
using MmoGame3d.Data.Players;
using MmoGame3d.Rules.Events;

[Collection(DatabaseCollection.Name)]
public class WorldEventStoreTests
{
    private const string Swarm = "drone-swarm-meadows";

    private readonly TestDatabase _database;

    public WorldEventStoreTests(TestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public void TheDroneSwarmIsDefined()
    {
        WorldEventDefinition swarm = new WorldEventStore(_database.Database).Definitions().Single(d => d.Id == Swarm);

        Assert.Equal(WorldEventDefinition.AiSwarm, swarm.Family);
        Assert.Equal("meadows", swarm.Zone);
    }

    [Fact]
    public void ARunLeftRunningAtARestartEndsAsSuch()
    {
        WorldEventStore store = new WorldEventStore(_database.Database);
        Guid ada = NewPlayer("Ada");
        long run = store.Start(Swarm);

        Assert.True(store.EndLeftRunning() >= 1);

        WorldEventRecord record = store.Recent(ada, 10).Single(r => r.Id == run);
        Assert.False(record.Running);
        Assert.Equal("ended-by-restart", record.Outcome);
    }

    [Fact]
    public void TakingPartGivesOnePointOncePerRun()
    {
        WorldEventStore store = new WorldEventStore(_database.Database);
        Guid ada = NewPlayer("Ada");
        long first = store.Start(Swarm);

        Assert.True(store.TakePart(first, ada));
        Assert.False(store.TakePart(first, ada));
        store.End(first, WorldEventOutcome.Completed);

        long second = store.Start(Swarm);
        store.TakePart(second, ada);

        Assert.Equal(2, store.Points(ada));

        List<WorldEventRecord> recent = store.Recent(ada, 10);
        Assert.True(recent.Single(r => r.Id == second).Running);
        WorldEventRecord ended = recent.Single(r => r.Id == first);
        Assert.Equal("completed", ended.Outcome);
        Assert.True(ended.TookPart);
        Assert.False(store.Recent(NewPlayer("Bo"), 10).Single(r => r.Id == first).TookPart);
    }

    private Guid NewPlayer(string name)
    {
        Guid accountId = new AccountStore(_database.Database).GetOrCreate(Guid.NewGuid());
        return new PlayerStore(_database.Database).GetOrCreate(
            new PlayerRecord { PlayerId = Guid.NewGuid(), AccountId = accountId, DisplayName = name, Zone = "town" }, out _).PlayerId;
    }
}
