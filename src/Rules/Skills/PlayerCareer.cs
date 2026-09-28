namespace MmoGame3d.Rules.Skills;

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
