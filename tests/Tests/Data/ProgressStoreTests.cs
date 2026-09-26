namespace MmoGame3d.Tests.Data;

using MmoGame3d.Data.Accounts;
using MmoGame3d.Data.Players;
using MmoGame3d.Data.Progress;
using MmoGame3d.Rules.Skills;

[Collection(DatabaseCollection.Name)]
public class ProgressStoreTests
{
    private readonly TestDatabase _database;

    public ProgressStoreTests(TestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public void SkillsAndCareerComeBackAsSaved()
    {
        Guid playerId = NewPlayer();
        ProgressStore store = new ProgressStore(_database.Database);
        PlayerProgress progress = new PlayerProgress { SecondsPlayed = 125, Missions = 2 };
        progress.Skills.Load(SkillId.Hacking, SkillCatalog.XpForLevel(4));
        progress.Career.TakeClass("college");
        progress.Career.Enroll(CareerCatalog.Get(CareerId.ComputerScientist), progress.Skills);
        progress.Career.AddCareerXp(40);

        store.Save(playerId, progress);
        store.Save(playerId, progress);
        PlayerProgress loaded = store.Load(playerId);

        Assert.Equal(progress.Skills.Xp(SkillId.Hacking), loaded.Skills.Xp(SkillId.Hacking));
        Assert.Equal(CareerId.ComputerScientist, loaded.Career.Career);
        Assert.Equal(40, loaded.Career.Xp);
        Assert.Equal("college", loaded.Career.College);
        Assert.Equal(2, loaded.Missions);
    }

    [Fact]
    public void ANewPlayerHasNothingYet()
    {
        PlayerProgress loaded = new ProgressStore(_database.Database).Load(NewPlayer());

        Assert.Null(loaded.Career.Career);
        Assert.False(loaded.Career.ClassTaken);
        Assert.Equal(0, loaded.Skills.TotalXp());
    }

    private Guid NewPlayer()
    {
        Guid accountId = new AccountStore(_database.Database).GetOrCreate(Guid.NewGuid());
        return new PlayerStore(_database.Database).GetOrCreate(
            new PlayerRecord { PlayerId = Guid.NewGuid(), AccountId = accountId, DisplayName = "Learner", Zone = "town" }, out _).PlayerId;
    }
}
