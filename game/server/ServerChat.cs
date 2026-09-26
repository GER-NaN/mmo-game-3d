namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Networking;
using MmoGame3d.Rules.Chat;

/// <summary>
/// Global chat: a line from a player goes through the filters and the player's rate
/// limit, then to everyone in the world. The server also speaks here for joins and
/// leaves. Lines reach only players in the world, never a client still at login.
/// </summary>
public class ServerChat
{
    // Anything longer is not a chat line; it is dropped before the filters see it.
    private const int MaxRawLength = 1000;

    private readonly Network _network;
    private readonly Func<IEnumerable<Session>> _sessions;
    private readonly ChatFilterPipeline _filters = ChatFilterPipeline.Default();
    private readonly Dictionary<long, ChatRateLimit> _limits = new Dictionary<long, ChatRateLimit>();

    public ServerChat(Network network, Func<IEnumerable<Session>> sessions)
    {
        _network = network;
        _sessions = sessions;
    }

    public void Say(Session speaker, string text)
    {
        string? clean = Prepare(speaker, text);

        if (clean != null)
        {
            Broadcast(speaker.Record!.DisplayName, clean, ChatKind.Say);
        }
    }

    // A line for some players only (a party). It passes the same filters and the same
    // rate limit as a line to everyone.
    public void SayTo(Session speaker, string text, ChatKind kind, IEnumerable<Session> listeners)
    {
        string? clean = Prepare(speaker, text);

        if (clean == null)
        {
            return;
        }

        foreach (Session listener in listeners)
        {
            if (listener.State == SessionState.InWorld)
            {
                _network.SendChatLine(listener.PeerId, speaker.Record!.DisplayName, clean, (int)kind);
            }
        }
    }

    // The line as it may go out, or null when it may not.
    private string? Prepare(Session speaker, string text)
    {
        if (speaker.State != SessionState.InWorld || speaker.Record == null || text.Length > MaxRawLength)
        {
            return null;
        }

        string clean = _filters.Apply(text);

        if (clean.Length == 0)
        {
            return null;
        }

        ChatRateLimit? limit;

        if (!_limits.TryGetValue(speaker.PeerId, out limit))
        {
            limit = new ChatRateLimit();
            _limits[speaker.PeerId] = limit;
        }

        if (!limit.TryTake(Time.GetTicksMsec() / 1000.0))
        {
            _network.SendNotice(speaker.PeerId, "You are sending too fast. Wait a moment.");
            return null;
        }

        return clean;
    }

    public void Announce(string text)
    {
        Broadcast("", text, ChatKind.System);
    }

    public void Forget(long peer)
    {
        _limits.Remove(peer);
    }

    private void Broadcast(string sender, string text, ChatKind kind)
    {
        foreach (Session session in _sessions())
        {
            if (session.State == SessionState.InWorld)
            {
                _network.SendChatLine(session.PeerId, sender, text, (int)kind);
            }
        }
    }
}
