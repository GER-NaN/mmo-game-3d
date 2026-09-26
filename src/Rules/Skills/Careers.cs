namespace MmoGame3d.Rules.Skills;

/// <summary>
/// Careers (docs/features/player-skills.md): broad, one at a time, optional. The list is
/// open; these two are built first. Ids go to the database and the wire: append only.
/// </summary>
public enum CareerId
{
    MechanicalEngineer = 0,
    ComputerScientist = 1,
}

public enum CareerRank
{
    Apprentice = 0,
    Graduate = 1,
    Senior = 2,
    Master = 3,
    Elite = 4,
}

public class CareerDefinition
{
    public CareerDefinition(CareerId id, string name, string summary, Dictionary<SkillId, int> gate)
    {
        Id = id;
        Name = name;
        Summary = summary;
        Gate = gate;
    }

    public CareerId Id { get; }
    public string Name { get; }
    public string Summary { get; }

    // The supporting skills and the level each needs before the career can start. The
    // supporting skills also feed the career's experience.
    public Dictionary<SkillId, int> Gate { get; }
}

public static class CareerCatalog
{
    // Career experience a rank needs, counted from the start of the career. Placeholders.
    private static readonly long[] RankXp = { 0, 150, 500, 1200, 2500 };

    // The gate levels are placeholders, low enough to reach in a first evening.
    public static readonly CareerDefinition[] All =
    {
        new CareerDefinition(
            CareerId.MechanicalEngineer,
            "Mechanical Engineer",
            "Keeps things running. Carries a repair pack: workbench work anywhere.",
            new Dictionary<SkillId, int> { { SkillId.Workbench, 3 }, { SkillId.FieldRepair, 3 }, { SkillId.ElectricalRepair, 2 } }),
        new CareerDefinition(
            CareerId.ComputerScientist,
            "Computer Scientist",
            "At home in the terminal. Sees more in it than others do.",
            new Dictionary<SkillId, int> { { SkillId.Hacking, 4 } }),
    };

    public static CareerDefinition? Find(int id)
    {
        foreach (CareerDefinition career in All)
        {
            if ((int)career.Id == id)
            {
                return career;
            }
        }

        return null;
    }

    public static CareerDefinition Get(CareerId id)
    {
        return Find((int)id)!;
    }

    public static string RankName(CareerRank rank)
    {
        return rank.ToString();
    }

    public static long XpForRank(CareerRank rank)
    {
        return RankXp[(int)rank];
    }

    // What someone else sees: "Mechanical Engineer · Senior", or "" without a career.
    public static string Title(CareerId? career, CareerRank rank)
    {
        return career.HasValue ? Get(career.Value).Name + " · " + RankName(rank) : "";
    }
}

/// <summary>
/// One player's career. A career starts at a college, after a one-time Class; the first
/// college is the player's own, and every rank-up is taken there, with their old
/// professor. Changing career loses its progress; skills stay.
/// </summary>
public class PlayerCareer
{
    public CareerId? Career { get; private set; }
    public long Xp { get; private set; }
    public CareerRank Rank { get; private set; }

    // The Class is a tutorial, taken once, before the first career.
    public bool ClassTaken { get; private set; }

    // The college the player took the Class at; "" before that.
    public string College { get; private set; } = "";

    public void Load(CareerId? career, long xp, CareerRank rank, bool classTaken, string college)
    {
        Career = career;
        Xp = xp;
        Rank = rank;
        ClassTaken = classTaken;
        College = college;
    }

    public void TakeClass(string college)
    {
        if (!ClassTaken)
        {
            ClassTaken = true;
            College = college;
        }
    }

    // The gate skills still below their level, as "Workbench 2/3". Empty when open.
    public static List<string> Missing(CareerDefinition career, SkillBook skills)
    {
        List<string> missing = new List<string>();

        foreach (KeyValuePair<SkillId, int> need in career.Gate)
        {
            int level = skills.Level(need.Key);

            if (level < need.Value)
            {
                missing.Add(SkillCatalog.Name(need.Key) + " " + level + "/" + need.Value);
            }
        }

        return missing;
    }

    // Starting a career or changing to another. Null when done, or the reason not.
    public string? Enroll(CareerDefinition career, SkillBook skills)
    {
        if (!ClassTaken)
        {
            return "Take the Class first.";
        }

        if (Career == career.Id)
        {
            return "You are already a " + career.Name + ".";
        }

        List<string> missing = Missing(career, skills);

        if (missing.Count > 0)
        {
            return career.Name + " needs " + string.Join(", ", missing) + ".";
        }

        Career = career.Id;
        Xp = 0;
        Rank = CareerRank.Apprentice;
        return null;
    }

    // Experience from a supporting skill, or from the career's own abilities.
    public void AddSkillXp(SkillId skill, long xp)
    {
        if (Career.HasValue && CareerCatalog.Get(Career.Value).Gate.ContainsKey(skill))
        {
            Xp += xp;
        }
    }

    public void AddCareerXp(long xp)
    {
        if (Career.HasValue)
        {
            Xp += xp;
        }
    }

    public bool CanRankUp()
    {
        return Career.HasValue && Rank < CareerRank.Elite && Xp >= CareerCatalog.XpForRank(Rank + 1);
    }

    // Null when done, or the reason not.
    public string? RankUp(string atCollege)
    {
        if (!Career.HasValue)
        {
            return "You have no career yet.";
        }

        if (atCollege != College)
        {
            return "Your old professor is at your own college.";
        }

        if (Rank == CareerRank.Elite)
        {
            return "There is no rank above Elite.";
        }

        if (!CanRankUp())
        {
            return "You need " + (CareerCatalog.XpForRank(Rank + 1) - Xp) + " more career experience for " + CareerCatalog.RankName(Rank + 1) + ".";
        }

        Rank = Rank + 1;
        return null;
    }

    // From 0 to 1 on the way to the next rank; 1 at Elite.
    public float Progress()
    {
        if (!Career.HasValue || Rank == CareerRank.Elite)
        {
            return Career.HasValue ? 1f : 0f;
        }

        long from = CareerCatalog.XpForRank(Rank);
        long to = CareerCatalog.XpForRank(Rank + 1);
        return Math.Clamp((Xp - from) / (float)(to - from), 0f, 1f);
    }
}

/// <summary>
/// The player level: time played, skill experience, career progress and missions, added
/// up (player-skills.md F5). The formula is a placeholder until the numbers session.
/// </summary>
public static class PlayerLevel
{
    public const long PointsPerMission = 100;
    public const double PointsPerLevelStep = 100;

    public static int For(long minutesPlayed, long skillXp, long careerXp, long missions)
    {
        long points = minutesPlayed + skillXp + careerXp + (missions * PointsPerMission);
        return 1 + (int)Math.Floor(Math.Sqrt(Math.Max(0, points) / PointsPerLevelStep));
    }
}

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
}
