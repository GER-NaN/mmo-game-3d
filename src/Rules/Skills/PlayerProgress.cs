namespace MmoGame3d.Rules.Skills;

/// <summary>
/// Everything a player progresses in, together: skills, career, and what the player
/// level counts besides them. Time played is kept in seconds and shown in minutes.
/// </summary>
public class PlayerProgress
{
    public SkillBook Skills { get; } = new SkillBook();
    public PlayerCareer Career { get; } = new PlayerCareer();
    public double SecondsPlayed { get; set; }

    // Jobs finished: the street-light repair today, missions later.
    public long Missions { get; set; }

    public int Level()
    {
        return PlayerLevel.For((long)(SecondsPlayed / 60), Skills.TotalXp(), Career.Xp, Missions);
    }

    // A copy for the persistence worker, which must not read what the game thread is
    // still changing.
    public PlayerProgress Copy()
    {
        PlayerProgress copy = new PlayerProgress { SecondsPlayed = SecondsPlayed, Missions = Missions };

        foreach (SkillId skill in SkillCatalog.All)
        {
            copy.Skills.Load(skill, Skills.Xp(skill));
        }

        copy.Career.Load(Career.Career, Career.Xp, Career.Rank, Career.ClassTaken, Career.College);
        return copy;
    }
}
