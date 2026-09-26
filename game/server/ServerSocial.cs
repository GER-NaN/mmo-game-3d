namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Data;
using MmoGame3d.Data.Social;
using MmoGame3d.Networking;
using MmoGame3d.Rules.Social;

/// <summary>
/// Friends and ignores. The lists live on the session, loaded with the player; each
/// change is written through the persistence worker at once, so there is nothing to
/// save later. Friends are told when a friend comes online or goes offline. What an
/// ignore blocks (chat, invites, gifts) is checked where those happen, through Ignores.
/// </summary>
public class ServerSocial
{
    private readonly SocialNetwork _network;
    private readonly Network _session;
    private readonly PersistenceWorker _worker;
    private readonly ContactStore _store;
    private readonly Func<IEnumerable<Session>> _sessions;
    private readonly Func<long, Session?> _findSession;

    public ServerSocial(SocialNetwork network, Network session, PersistenceWorker worker, ContactStore store, Func<IEnumerable<Session>> sessions, Func<long, Session?> findSession)
    {
        _network = network;
        _session = session;
        _worker = worker;
        _store = store;
        _sessions = sessions;
        _findSession = findSession;
    }

    // True when the listener ignores the one speaking, inviting or giving.
    public static bool Ignores(Session listener, Session other)
    {
        return other.Record != null && listener.Contacts.Ignores(other.Record.PlayerId);
    }

    public void Befriend(Session session, long targetPeer)
    {
        Session? target = _findSession(targetPeer);

        if (target == null || target.Record == null)
        {
            _session.SendNotice(session.PeerId, "There is nobody there to add.");
            return;
        }

        Guid me = session.Record!.PlayerId;
        Guid them = target.Record.PlayerId;
        string? refusal = session.Contacts.Befriend(me, them, target.Record.DisplayName);

        if (refusal != null)
        {
            _session.SendNotice(session.PeerId, refusal);
            return;
        }

        _worker.Enqueue(() => _store.Set(me, them, false), e => GD.PrintErr("Saving a friend failed: " + e.Message));
        _session.SendNotice(session.PeerId, target.Record.DisplayName + " is now your friend.");
        Send(session);
    }

    // From a Whois page: by player id, online or not. The name comes from the database,
    // never from the client.
    public void BefriendId(Session session, string playerIdText)
    {
        Guid them;

        if (!Guid.TryParse(playerIdText, out them))
        {
            return;
        }

        foreach (Session other in _sessions())
        {
            if (other.Record != null && other.Record.PlayerId == them)
            {
                Befriend(session, other.PeerId);
                return;
            }
        }

        _worker.Enqueue(
            () => _store.NameOf(them),
            name =>
            {
                if (name == null)
                {
                    return;
                }

                Guid me = session.Record!.PlayerId;
                string? refusal = session.Contacts.Befriend(me, them, name);

                if (refusal != null)
                {
                    _session.SendNotice(session.PeerId, refusal);
                    return;
                }

                _worker.Enqueue(() => _store.Set(me, them, false), e => GD.PrintErr("Saving a friend failed: " + e.Message));
                _session.SendNotice(session.PeerId, name + " is now your friend.");
                Send(session);
            },
            e => GD.PrintErr("Finding a friend failed: " + e.Message));
    }

    public void Ignore(Session session, long targetPeer)
    {
        Session? target = _findSession(targetPeer);

        if (target == null || target.Record == null)
        {
            _session.SendNotice(session.PeerId, "There is nobody there to ignore.");
            return;
        }

        Guid me = session.Record!.PlayerId;
        Guid them = target.Record.PlayerId;
        string? refusal = session.Contacts.Ignore(me, them, target.Record.DisplayName);

        if (refusal != null)
        {
            _session.SendNotice(session.PeerId, refusal);
            return;
        }

        _worker.Enqueue(() => _store.Set(me, them, true), e => GD.PrintErr("Saving an ignore failed: " + e.Message));
        _session.SendNotice(session.PeerId, "You ignore " + target.Record.DisplayName + ": no chat, invites or gifts from them.");
        Send(session);
    }

    public void Remove(Session session, string playerIdText)
    {
        Guid them;

        if (!Guid.TryParse(playerIdText, out them) || !session.Contacts.Remove(them))
        {
            return;
        }

        Guid me = session.Record!.PlayerId;
        _worker.Enqueue(() => _store.Remove(me, them), e => GD.PrintErr("Removing a contact failed: " + e.Message));
        Send(session);
    }

    // The first time into the world this session: their own list, and a word to anyone
    // who has them as a friend.
    public void CameOnline(Session session)
    {
        Send(session);
        TellFriendsOf(session, " is online.");
    }

    public void WentOffline(Session session)
    {
        TellFriendsOf(session, " went offline.");
    }

    // Zones change the list too, so it is sent again on every arrival.
    public void Arrived(Session session)
    {
        foreach (Session other in _sessions())
        {
            if (other != session && other.State == SessionState.InWorld && other.Contacts.IsFriend(session.Record!.PlayerId))
            {
                Send(other);
            }
        }
    }

    public void Send(Session session)
    {
        Dictionary<Guid, string> zones = new Dictionary<Guid, string>();

        foreach (Session other in _sessions())
        {
            if (other.State == SessionState.InWorld && other.Record != null)
            {
                zones[other.Record.PlayerId] = other.Record.Zone;
            }
        }

        List<string> friendIds = new List<string>();
        List<string> friendNames = new List<string>();
        List<string> friendZones = new List<string>();
        List<string> ignoredIds = new List<string>();
        List<string> ignoredNames = new List<string>();

        foreach (KeyValuePair<Guid, string> friend in session.Contacts.Friends)
        {
            string? zone;
            friendIds.Add(friend.Key.ToString());
            friendNames.Add(friend.Value);
            friendZones.Add(zones.TryGetValue(friend.Key, out zone) ? zone : "");
        }

        foreach (KeyValuePair<Guid, string> pest in session.Contacts.Ignored)
        {
            ignoredIds.Add(pest.Key.ToString());
            ignoredNames.Add(pest.Value);
        }

        _network.SendContacts(session.PeerId, friendIds.ToArray(), friendNames.ToArray(), friendZones.ToArray(), ignoredIds.ToArray(), ignoredNames.ToArray());
    }

    private void TellFriendsOf(Session session, string what)
    {
        foreach (Session other in _sessions())
        {
            if (other != session && other.State == SessionState.InWorld && other.Contacts.IsFriend(session.Record!.PlayerId))
            {
                _session.SendNotice(other.PeerId, session.Record.DisplayName + what);
                Send(other);
            }
        }
    }
}
