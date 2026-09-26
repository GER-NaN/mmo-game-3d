namespace MmoGame3d.Rules.Skills;

/// <summary>
/// Skills (a working name, docs/features/player-skills.md): narrow, one kind of
/// activity each, earned by doing the thing. Only skills the game can earn today are
/// here; the design names more (Drone Control, Networking, Social) that come with their
/// mechanics. The numbers are ids in the database and on the wire: append, never reuse.
/// </summary>
public enum SkillId
{
    Agility = 0,
    Hacking = 1,
    Workbench = 2,
    FieldRepair = 3,
    ElectricalRepair = 4,
}

public static class SkillCatalog
{
    // The level curve and the top level are placeholders: level L needs
    // XpPerLevelStep * (L - 1)^2 experience, so early levels come quickly.
    public const int MaxLevel = 99;
    public const long XpPerLevelStep = 25;

    public static readonly SkillId[] All =
    {
        SkillId.Agility, SkillId.Hacking, SkillId.Workbench, SkillId.FieldRepair, SkillId.ElectricalRepair,
    };

    public static string Name(SkillId skill)
    {
        switch (skill)
        {
            case SkillId.Agility:
                return "Agility";
            case SkillId.Hacking:
                return "Hacking";
            case SkillId.Workbench:
                return "Workbench";
            case SkillId.FieldRepair:
                return "Field repair";
            case SkillId.ElectricalRepair:
                return "Electrical repair";
            default:
                return skill.ToString();
        }
    }

    // How the skill is earned, for the player.
    public static string HowEarned(SkillId skill)
    {
        switch (skill)
        {
            case SkillId.Agility:
                return "Walking and jumping.";
            case SkillId.Hacking:
                return "Cracking codes at public terminals.";
            case SkillId.Workbench:
                return "Work at a workbench.";
            case SkillId.FieldRepair:
                return "Fixing broken things around town.";
            case SkillId.ElectricalRepair:
                return "Repairing junction boxes.";
            default:
                return "";
        }
    }

    public static bool IsKnown(int id)
    {
        return Enum.IsDefined(typeof(SkillId), id);
    }

    public static int LevelFor(long xp)
    {
        int level = 1 + (int)Math.Floor(Math.Sqrt(Math.Max(0, xp) / (double)XpPerLevelStep));
        return Math.Min(level, MaxLevel);
    }

    public static long XpForLevel(int level)
    {
        long steps = Math.Max(0, level - 1);
        return XpPerLevelStep * steps * steps;
    }
}

/// <summary>
/// Experience per act. Placeholders until the numbers session.
/// </summary>
public static class SkillAwards
{
    public const float AgilityMetresPerXp = 25f;
    public const int AgilityJumpsPerXp = 3;
    public const long HackingPerCode = 20;
    public const long WorkbenchPerJob = 10;
    public const long FieldRepairPerFix = 15;
    public const long ElectricalRepairPerBox = 40;

    // Career experience for work done from the engineer's repair pack.
    public const long RepairPackPerJob = 10;
}

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

/// <summary>
/// Agility counts distance and jumps: each full step of either is one experience. The
/// remainders live for the session only; losing part of a step at logout is not worth
/// storing.
/// </summary>
public class AgilityCounter
{
    private float _metres;
    private int _jumps;

    // Experience earned by this stretch of walking.
    public long Walked(float metres)
    {
        _metres += metres;
        long xp = (long)(_metres / SkillAwards.AgilityMetresPerXp);
        _metres -= xp * SkillAwards.AgilityMetresPerXp;
        return xp;
    }

    public long Jumped()
    {
        _jumps++;

        if (_jumps < SkillAwards.AgilityJumpsPerXp)
        {
            return 0;
        }

        _jumps = 0;
        return 1;
    }
}
