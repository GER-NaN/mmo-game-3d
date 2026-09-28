namespace MmoGame3d.Rules.Skills;

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
