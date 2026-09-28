namespace MmoGame3d.Rules.Parties;

public class PartyInvite
{
    public PartyInvite(Guid inviterId, Guid targetId, double secondsRemaining)
    {
        InviterId = inviterId;
        TargetId = targetId;
        SecondsRemaining = secondsRemaining;
    }

    public Guid InviterId { get; }
    public Guid TargetId { get; }
    public double SecondsRemaining { get; set; }
}
