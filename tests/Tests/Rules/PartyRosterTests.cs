namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Parties;

public class PartyRosterTests
{
    private readonly Guid _alice = Guid.NewGuid();
    private readonly Guid _bob = Guid.NewGuid();
    private readonly Guid _carol = Guid.NewGuid();

    [Fact]
    public void AnAcceptedInviteMakesAPartyLedByTheInviter()
    {
        PartyRoster roster = new PartyRoster();

        Assert.True(roster.Invite(_alice, _bob));
        Assert.True(roster.Accept(_bob, "Bob", _alice, "Alice"));

        Party party = roster.FindForPlayer(_bob)!;
        Assert.Equal(_alice, party.LeaderId);
        Assert.True(roster.AreInSameParty(_alice, _bob));
    }

    [Fact]
    public void AcceptingWithoutAnInviteDoesNothing()
    {
        PartyRoster roster = new PartyRoster();

        Assert.False(roster.Accept(_bob, "Bob", _alice, "Alice"));
        Assert.Null(roster.FindForPlayer(_bob));
    }

    [Fact]
    public void APartyOfOneDisbands()
    {
        PartyRoster roster = PartyOfAliceAndBob();

        roster.Leave(_bob);

        Assert.Null(roster.FindForPlayer(_alice));
    }

    [Fact]
    public void LeadershipPassesWhenTheLeaderLeaves()
    {
        PartyRoster roster = PartyOfAliceAndBob();
        roster.Invite(_alice, _carol);
        roster.Accept(_carol, "Carol", _alice, "Alice");

        roster.Leave(_alice);

        Assert.Equal(_bob, roster.FindForPlayer(_carol)!.LeaderId);
    }

    [Fact]
    public void ADisconnectKeepsTheMembership()
    {
        PartyRoster roster = PartyOfAliceAndBob();

        roster.PlayerLeftWorld(_bob);

        Assert.True(roster.AreInSameParty(_alice, _bob));
    }

    [Fact]
    public void AnInviteExpires()
    {
        PartyRoster roster = new PartyRoster();
        roster.Invite(_alice, _bob);

        roster.Tick(PartyRoster.InviteLifetimeSeconds + 1);

        Assert.False(roster.Accept(_bob, "Bob", _alice, "Alice"));
    }

    [Fact]
    public void AMemberOfAPartyCannotBeInvitedToAnother()
    {
        PartyRoster roster = PartyOfAliceAndBob();

        Assert.False(roster.Invite(_carol, _bob));
    }

    private PartyRoster PartyOfAliceAndBob()
    {
        PartyRoster roster = new PartyRoster();
        roster.Invite(_alice, _bob);
        roster.Accept(_bob, "Bob", _alice, "Alice");
        return roster;
    }
}
