namespace MmoGame3d.Rules.Skills;

/// <summary>
/// One player's skills: experience per skill, and the level that follows from it.
/// </summary>
public class SkillBook
{
    private readonly Dictionary<SkillId, long> _xp = new Dictionary<SkillId, long>();

    public long Xp(SkillId skill)
    {
        long xp;
        return _xp.TryGetValue(skill, out xp) ? xp : 0;
    }

    public int Level(SkillId skill)
    {
        return SkillCatalog.LevelFor(Xp(skill));
    }

    public long TotalXp()
    {
        long total = 0;

        foreach (long xp in _xp.Values)
        {
            total += xp;
        }

        return total;
    }

    public void Load(SkillId skill, long xp)
    {
        _xp[skill] = xp;
    }

    // The new level when the award crossed one, or 0.
    public int Add(SkillId skill, long xp)
    {
        int before = Level(skill);
        _xp[skill] = Xp(skill) + xp;
        int after = Level(skill);
        return after > before ? after : 0;
    }
}
