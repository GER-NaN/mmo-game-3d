namespace MmoGame3d.Rules.Parties;

public class PartyMember
{
    public PartyMember(Guid playerId, string name)
    {
        PlayerId = playerId;
        Name = name;
    }

    public Guid PlayerId { get; }
    public string Name { get; }
}
