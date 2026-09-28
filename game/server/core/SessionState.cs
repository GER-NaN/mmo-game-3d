namespace MmoGame3d.Server;
public enum SessionState
{
    // Connected, no hello yet.
    Connected,

    // The account is known; the player is choosing or making a character.
    Choosing,

    // Login sent, the account lookup is running.
    LoggingIn,

    // Accepted; the client is loading its zone.
    Accepted,

    // The body is in the world.
    InWorld,
}
