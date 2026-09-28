namespace MmoGame3d.Rules.Skills;

public static class SkillCatalog
{
    // The level curve and the top level are placeholders: level L needs
    // XpPerLevelStep * (L - 1)^2 experience, so early levels come quickly.
    public const int MaxLevel = 99;
    public const long XpPerLevelStep = 25;

    public static readonly SkillId[] All =
    {
        SkillId.Agility, SkillId.Hacking, SkillId.Workbench, SkillId.FieldRepair, SkillId.ElectricalRepair, SkillId.Gardening,
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
            case SkillId.Gardening:
                return "Gardening";
            default:
                return skill.ToString();
        }
    }

    // What the skill is, for the player. Placeholder wording.
    public static string About(SkillId skill)
    {
        switch (skill)
        {
            case SkillId.Agility:
                return "How well you get about on foot.";
            case SkillId.Hacking:
                return "Getting into the AI's systems and throwing it out of the town's.";
            case SkillId.Workbench:
                return "Fitting and servicing your own equipment.";
            case SkillId.FieldRepair:
                return "Mending the small things round town that break: benches, hydrants, traffic lights, dumpster lids.";
            case SkillId.ElectricalRepair:
                return "Keeping the town's wiring working: the street lights, fed from a junction box (the lamps are not fixed one by one), and the security cameras on building corners.";
            case SkillId.Gardening:
                return "Growing things: house plants from the potting table.";
            default:
                return "";
        }
    }

    // How the skill is earned, for the player: what to do and where.
    public static string HowEarned(SkillId skill)
    {
        switch (skill)
        {
            case SkillId.Agility:
                return "Walk and jump anywhere.";
            case SkillId.Hacking:
                return "Crack codes and play Agent Defense on a public terminal; clean the rootkit out of the robo taxis when Town repairs has that job.";
            case SkillId.Workbench:
                return "Change a phone's battery and do other work at a workbench (the shop has one), or with the engineer's repair pack.";
            case SkillId.FieldRepair:
                return "Look for sparks: walk up to the broken bench, hydrant, traffic light or dumpster lid and fix it.";
            case SkillId.ElectricalRepair:
                return "Fix a sparking security camera on a building corner. When the AI takes out the street lights, take the job in Town repairs on a terminal, carry a RAM stick, and repair the junction box on Main Street.";
            case SkillId.Gardening:
                return "Make a house plant at the potting table in the greenhouse, off the outskirts.";
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
