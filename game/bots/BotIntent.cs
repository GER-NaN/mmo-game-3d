namespace MmoGame3d.Bots;

/// <summary>
/// What a step is doing with the player's body, for those that watch it: a watcher
/// judges "not moving" only while walking, and chat is typed only while the keys are
/// not needed for something else.
/// </summary>
public enum BotIntent
{
    // Waiting, checking, standing: the keys are free.
    Idle,

    // Walking somewhere: it should be moving.
    Walking,

    // Pushing into something on purpose (an edge, a gap): not moving is the point.
    Pushing,

    // Working a screen: its keys and clicks belong to it.
    Screen,
}
