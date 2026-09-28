namespace MmoGame3d.Rules.Terminals;

// Whether an app can be opened, and if not, why.
public enum AppState
{
    Open,

    // In the game, not reachable yet: shown with a locked notice, so a player sees what
    // exists before they can use it.
    Locked,
}
