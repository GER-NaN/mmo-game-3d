namespace MmoGame3d.Server;

using MmoGame3d.Data.Players;
using MmoGame3d.Players;

/// <summary>
/// One connected client, from connect to disconnect. The peer id is the session's
/// identity: ENet binds it to the connection, and Godot stamps it on every RPC as the
/// sender, so a client cannot claim another's. That is why there is no session token.
/// </summary>
public class Session
{
    public Session(long peerId)
    {
        PeerId = peerId;
    }

    public long PeerId { get; }

    public SessionState State { get; set; } = SessionState.Connected;

    // Set once the account lookup is back.
    public PlayerRecord? Record { get; set; }

    // Set once the client has its world loaded and the body is spawned.
    public Player? Body { get; set; }

    public string? ZoneId
    {
        get { return Record?.Zone; }
    }
}

public enum SessionState
{
    // Connected, no login yet.
    Connected,

    // Login sent, the account lookup is running.
    LoggingIn,

    // Accepted; the client is loading its zone.
    Accepted,

    // The body is in the world.
    InWorld,
}
