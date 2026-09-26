namespace MmoGame3d.Rules.Parties;

/// <summary>
/// Who is in a party with whom, and who was asked. Ported from mmo-game, where these
/// rules were decided: a party holds until a member presses Leave (neither distance nor
/// a disconnect removes you), one party at a time, and a party of one disbands.
///
/// It knows players by their persistent player id, so a member who reconnects is still
/// a member. Names are passed in and kept, because a party outlives its members'
/// presence in the world and the roster has nowhere to look them up from.
/// </summary>
public class PartyRoster
{
    public const int MaxMembers = 6;

    // Long enough to read a prompt, short enough that a forgotten invite does not follow
    // a player around.
    public const double InviteLifetimeSeconds = 60;

    private readonly Dictionary<Guid, Party> _partiesById = new Dictionary<Guid, Party>();
    private readonly Dictionary<Guid, Guid> _partyIdByMember = new Dictionary<Guid, Guid>();
    private readonly List<PartyInvite> _invites = new List<PartyInvite>();
    private readonly HashSet<Guid> _changed = new HashSet<Guid>();

    public Party? FindForPlayer(Guid playerId)
    {
        Guid partyId;

        if (!_partyIdByMember.TryGetValue(playerId, out partyId))
        {
            return null;
        }

        return _partiesById[partyId];
    }

    public bool AreInSameParty(Guid playerId, Guid otherPlayerId)
    {
        Guid partyId;
        Guid otherPartyId;

        return _partyIdByMember.TryGetValue(playerId, out partyId)
            && _partyIdByMember.TryGetValue(otherPlayerId, out otherPartyId)
            && partyId == otherPartyId;
    }

    public bool Invite(Guid inviterId, Guid targetId)
    {
        if (inviterId == targetId)
        {
            return false;
        }

        // One party at a time. Someone who wants a different party leaves theirs first,
        // so an invite can never move a member out from under the party they are in.
        if (_partyIdByMember.ContainsKey(targetId))
        {
            return false;
        }

        Party? party = FindForPlayer(inviterId);

        if (party != null && party.Members.Count >= MaxMembers)
        {
            return false;
        }

        PartyInvite? existing = FindInvite(inviterId, targetId);

        if (existing != null)
        {
            existing.SecondsRemaining = InviteLifetimeSeconds;
            return true;
        }

        _invites.Add(new PartyInvite(inviterId, targetId, InviteLifetimeSeconds));
        return true;
    }

    public bool Accept(Guid targetId, string targetName, Guid inviterId, string inviterName)
    {
        PartyInvite? invite = FindInvite(inviterId, targetId);

        if (invite == null)
        {
            return false;
        }

        _invites.Remove(invite);

        // The world moved while the invite waited: the target joined someone else, or the
        // inviter's party filled up. Either way the answer arrives too late.
        if (_partyIdByMember.ContainsKey(targetId))
        {
            return false;
        }

        Party? party = FindForPlayer(inviterId);

        if (party == null)
        {
            party = new Party(Guid.NewGuid(), inviterId);
            _partiesById.Add(party.PartyId, party);
            AddMember(party, inviterId, inviterName);
        }
        else if (party.Members.Count >= MaxMembers)
        {
            return false;
        }

        AddMember(party, targetId, targetName);
        MarkChanged(party);
        return true;
    }

    public void Decline(Guid targetId, Guid inviterId)
    {
        PartyInvite? invite = FindInvite(inviterId, targetId);

        if (invite != null)
        {
            _invites.Remove(invite);
        }
    }

    public void Leave(Guid playerId)
    {
        // Invites this player sent belong to the party they are leaving. Dropping them
        // stops a late acceptance from making a surprise second party.
        _invites.RemoveAll(invite => invite.InviterId == playerId);

        Party? party = FindForPlayer(playerId);

        if (party == null)
        {
            return;
        }

        party.Members.RemoveAll(member => member.PlayerId == playerId);
        _partyIdByMember.Remove(playerId);
        _changed.Add(playerId);

        // A party of one is not a party: the last member could do nothing in it that a
        // lone player cannot.
        if (party.Members.Count <= 1)
        {
            foreach (PartyMember member in party.Members)
            {
                _partyIdByMember.Remove(member.PlayerId);
                _changed.Add(member.PlayerId);
            }

            _partiesById.Remove(party.PartyId);
            return;
        }

        if (party.LeaderId == playerId)
        {
            party.LeaderId = party.Members[0].PlayerId;
        }

        MarkChanged(party);
    }

    // A disconnect does not leave a party, but invites to and from the player go: one
    // nobody is there to answer, and one whose sender is gone. The rest of the party is
    // told, since who is online is part of what they see.
    public void PlayerLeftWorld(Guid playerId)
    {
        _invites.RemoveAll(invite => invite.InviterId == playerId || invite.TargetId == playerId);
        MarkChangedFor(playerId);
    }

    // A returning member has lost what their client knew, and the others see them online.
    public void PlayerEnteredWorld(Guid playerId)
    {
        MarkChangedFor(playerId);
    }

    public void Tick(double deltaSeconds)
    {
        for (int i = _invites.Count - 1; i >= 0; i--)
        {
            _invites[i].SecondsRemaining -= deltaSeconds;

            if (_invites[i].SecondsRemaining <= 0)
            {
                _invites.RemoveAt(i);
            }
        }
    }

    // The players whose view of their party changed since the last call; the caller
    // sends each of them their party.
    public List<Guid> TakeChanged()
    {
        List<Guid> changed = new List<Guid>(_changed);
        _changed.Clear();
        return changed;
    }

    private void MarkChangedFor(Guid playerId)
    {
        Party? party = FindForPlayer(playerId);

        if (party != null)
        {
            MarkChanged(party);
        }
    }

    private void AddMember(Party party, Guid playerId, string name)
    {
        party.Members.Add(new PartyMember(playerId, name));
        _partyIdByMember.Add(playerId, party.PartyId);
    }

    private void MarkChanged(Party party)
    {
        foreach (PartyMember member in party.Members)
        {
            _changed.Add(member.PlayerId);
        }
    }

    private PartyInvite? FindInvite(Guid inviterId, Guid targetId)
    {
        foreach (PartyInvite invite in _invites)
        {
            if (invite.InviterId == inviterId && invite.TargetId == targetId)
            {
                return invite;
            }
        }

        return null;
    }
}

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
