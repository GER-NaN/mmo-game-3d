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
    Gardening = 5,
}
