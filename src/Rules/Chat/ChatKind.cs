namespace MmoGame3d.Rules.Chat;

// Who a chat line is from, which decides how it is drawn.
public enum ChatKind
{
    // A player, to everyone online.
    Say,

    // The server: joins, leaves, announcements.
    System,

    // A player, to their party only.
    Party,
}
