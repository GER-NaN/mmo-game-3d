namespace MmoGame3d.Rules.Skills;

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
    public const long ElectricalRepairPerCamera = 20;

    // Career experience for work done from the engineer's repair pack.
    public const long RepairPackPerJob = 10;
}
