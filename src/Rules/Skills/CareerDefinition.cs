namespace MmoGame3d.Rules.Skills;

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
