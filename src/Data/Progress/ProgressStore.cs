namespace MmoGame3d.Data.Progress;

using System.Data;
using Dapper;
using MmoGame3d.Rules.Skills;

// Skills and progress, saved with the player: the whole of it each time, in one
// transaction, like the bag. Unknown skill or career ids (from a newer build) are
// skipped on load rather than failing the login.
public class ProgressStore
{
    private readonly Database _database;

    public ProgressStore(Database database)
    {
        _database = database;
    }

    public PlayerProgress Load(Guid playerId)
    {
        using IDbConnection connection = _database.Open();
        PlayerProgress progress = new PlayerProgress();

        foreach (SkillRow row in connection.Query<SkillRow>("select skill as Skill, xp as Xp from player_skills where player_id = @playerId;", new { playerId }))
        {
            if (SkillCatalog.IsKnown(row.Skill))
            {
                progress.Skills.Load((SkillId)row.Skill, row.Xp);
            }
        }

        ProgressRow? saved = connection.QuerySingleOrDefault<ProgressRow>(
            @"select career as Career, career_xp as CareerXp, career_rank as CareerRank, class_taken as ClassTaken,
                     college as College, seconds_played as SecondsPlayed, missions as Missions
              from player_progress where player_id = @playerId;",
            new { playerId });

        if (saved != null)
        {
            CareerId? career = saved.Career.HasValue && CareerCatalog.Find(saved.Career.Value) != null ? (CareerId)saved.Career.Value : null;
            progress.Career.Load(career, saved.CareerXp, (CareerRank)Math.Clamp(saved.CareerRank, 0, (int)CareerRank.Elite), saved.ClassTaken, saved.College);
            progress.SecondsPlayed = saved.SecondsPlayed;
            progress.Missions = saved.Missions;
        }

        return progress;
    }

    public void Save(Guid playerId, PlayerProgress progress)
    {
        using IDbConnection connection = _database.Open();
        using IDbTransaction transaction = connection.BeginTransaction();

        foreach (SkillId skill in SkillCatalog.All)
        {
            long xp = progress.Skills.Xp(skill);

            if (xp > 0)
            {
                connection.Execute(
                    @"insert into player_skills (player_id, skill, xp) values (@playerId, @skill, @xp)
                      on conflict (player_id, skill) do update set xp = excluded.xp;",
                    new { playerId, skill = (int)skill, xp },
                    transaction);
            }
        }

        PlayerCareer career = progress.Career;
        connection.Execute(
            @"insert into player_progress (player_id, career, career_xp, career_rank, class_taken, college, seconds_played, missions)
              values (@playerId, @career, @careerXp, @careerRank, @classTaken, @college, @secondsPlayed, @missions)
              on conflict (player_id) do update set
                  career = excluded.career, career_xp = excluded.career_xp, career_rank = excluded.career_rank,
                  class_taken = excluded.class_taken, college = excluded.college,
                  seconds_played = excluded.seconds_played, missions = excluded.missions;",
            new
            {
                playerId,
                career = career.Career.HasValue ? (int?)career.Career.Value : null,
                careerXp = career.Xp,
                careerRank = (int)career.Rank,
                classTaken = career.ClassTaken,
                college = career.College,
                secondsPlayed = progress.SecondsPlayed,
                missions = progress.Missions,
            },
            transaction);

        transaction.Commit();
    }

    private class SkillRow
    {
        public int Skill { get; set; }
        public long Xp { get; set; }
    }

    private class ProgressRow
    {
        public int? Career { get; set; }
        public long CareerXp { get; set; }
        public int CareerRank { get; set; }
        public bool ClassTaken { get; set; }
        public string College { get; set; } = "";
        public double SecondsPlayed { get; set; }
        public long Missions { get; set; }
    }
}
