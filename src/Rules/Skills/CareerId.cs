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
