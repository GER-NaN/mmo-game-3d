namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using MmoGame3d.Networking;
using MmoGame3d.Rules.Chat;
using MmoGame3d.Rules.Parties;

/// <summary>
/// Parties on the server: turns party requests into roster calls, and sends each member
/// their party whenever it changes. The rules live in PartyRoster; this is the plumbing
/// between it, the sessions and the wire. Parties live in memory: they survive a
/// member's disconnect, but not a server restart.
/// </summary>
public class ServerParties
{
    private readonly PartyRoster _roster = new PartyRoster();
    private readonly PartyNetwork _network;
    private readonly Network _session;
    private readonly ServerChat _chat;
    private readonly Func<IEnumerable<Session>> _sessions;

    public ServerParties(PartyNetwork network, Network session, ServerChat chat, Func<IEnumerable<Session>> sessions)
    {
        _network = network;
        _session = session;
        _chat = chat;
        _sessions = sessions;
    }

    public bool AreInSameParty(Guid playerId, Guid otherPlayerId)
    {
        return _roster.AreInSameParty(playerId, otherPlayerId);
    }

    public void Invite(Session inviter, Session? target)
    {
        if (inviter.Record == null || target == null || target.Record == null || target.State != SessionState.InWorld)
        {
            _session.SendNotice(inviter.PeerId, "There is nobody there to invite.");
            return;
        }

        if (!_roster.Invite(inviter.Record.PlayerId, target.Record.PlayerId))
        {
            _session.SendNotice(inviter.PeerId, target.Record.DisplayName + " cannot join your party now.");
            return;
        }

        _network.SendInvited(target.PeerId, inviter.Record.PlayerId.ToString(), inviter.Record.DisplayName);
        _session.SendNotice(inviter.PeerId, "Invited " + target.Record.DisplayName + ".");
    }

    public void Respond(Session target, string inviterIdText, bool accept)
    {
        Guid inviterId;

        if (target.Record == null || !Guid.TryParse(inviterIdText, out inviterId))
        {
            return;
        }

        if (!accept)
        {
            _roster.Decline(target.Record.PlayerId, inviterId);
            return;
        }

        // The inviter's name comes from their session: an invite dies with its sender's
        // session, so a live invite always has one.
        Session? inviter = FindByPlayer(inviterId);

        if (inviter == null || !_roster.Accept(target.Record.PlayerId, target.Record.DisplayName, inviterId, inviter.Record!.DisplayName))
        {
            _session.SendNotice(target.PeerId, "That invite is no longer open.");
        }
    }

    public void Leave(Session member)
    {
        if (member.Record != null)
        {
            _roster.Leave(member.Record.PlayerId);
        }
    }

    public void Chat(Session speaker, string text)
    {
        Party? party = speaker.Record == null ? null : _roster.FindForPlayer(speaker.Record.PlayerId);

        if (party == null)
        {
            _session.SendNotice(speaker.PeerId, "You are not in a party.");
            return;
        }

        List<Session> listeners = new List<Session>();

        foreach (PartyMember member in party.Members)
        {
            Session? listener = FindByPlayer(member.PlayerId);

            if (listener != null)
            {
                listeners.Add(listener);
            }
        }

        _chat.SayTo(speaker, text, ChatKind.Party, listeners);
    }

    // The other members of this player's party who are in the world now.
    public List<Session> OthersOnline(Session session)
    {
        List<Session> others = new List<Session>();
        Party? party = session.Record == null ? null : _roster.FindForPlayer(session.Record.PlayerId);

        if (party == null)
        {
            return others;
        }

        foreach (PartyMember member in party.Members)
        {
            Session? other = FindByPlayer(member.PlayerId);

            if (other != null && other != session && other.State == SessionState.InWorld)
            {
                others.Add(other);
            }
        }

        return others;
    }

    public void EnteredWorld(Session session)
    {
        _roster.PlayerEnteredWorld(session.Record!.PlayerId);
    }

    public void LeftWorld(Session session)
    {
        _roster.PlayerLeftWorld(session.Record!.PlayerId);
    }

    public void Tick(double delta)
    {
        _roster.Tick(delta);

        foreach (Guid playerId in _roster.TakeChanged())
        {
            Session? session = FindByPlayer(playerId);

            if (session != null && session.State == SessionState.InWorld)
            {
                SendParty(session);
            }
        }
    }

    private void SendParty(Session session)
    {
        Party? party = _roster.FindForPlayer(session.Record!.PlayerId);

        if (party == null)
        {
            _network.SendParty(session.PeerId, "", Array.Empty<string>(), Array.Empty<string>(), Array.Empty<int>());
            return;
        }

        string[] ids = new string[party.Members.Count];
        string[] names = new string[party.Members.Count];
        int[] online = new int[party.Members.Count];

        for (int i = 0; i < party.Members.Count; i++)
        {
            PartyMember member = party.Members[i];
            Session? memberSession = FindByPlayer(member.PlayerId);
            ids[i] = member.PlayerId.ToString();
            names[i] = member.Name;
            online[i] = memberSession != null && memberSession.State == SessionState.InWorld ? 1 : 0;
        }

        _network.SendParty(session.PeerId, party.LeaderId.ToString(), ids, names, online);
    }

    // A linear search: sessions number in the hundreds and parties change rarely.
    private Session? FindByPlayer(Guid playerId)
    {
        foreach (Session session in _sessions())
        {
            if (session.Record != null && session.Record.PlayerId == playerId)
            {
                return session;
            }
        }

        return null;
    }
}
