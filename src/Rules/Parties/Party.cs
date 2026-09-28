namespace MmoGame3d.Rules.Parties;

public class Party
{
    public Party(Guid partyId, Guid leaderId)
    {
        PartyId = partyId;
        LeaderId = leaderId;
    }

    public Guid PartyId { get; }
    public Guid LeaderId { get; set; }
    public List<PartyMember> Members { get; } = new List<PartyMember>();
}
