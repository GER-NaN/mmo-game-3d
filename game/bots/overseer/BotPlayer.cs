namespace MmoGame3d.Overseer;

/// <summary>
/// Which player a bot plays as.
/// </summary>
public enum BotPlayer
{
    // Not connected: the bot stays at the main menu and its screens.
    None,

    // A kept player of its own ("bot-<name>"), the same on every run.
    Kept,

    // A new player on every launch.
    Fresh,
}
