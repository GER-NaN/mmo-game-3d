namespace MmoGame3d.Players;

/// <summary>
/// Where a player's own body takes its walking from, in place of the keyboard. Only the
/// load test uses it: many bots share one process there, and Godot has one Input for
/// the whole process, so each bot needs input of its own. Everything after this (the
/// heading, the RPCs, the server's checks) is the same path a key press takes.
/// </summary>
public interface IPlayerInput
{
    // -1 (right) to 1 (left).
    float Turn { get; }

    // -1 (back) to 1 (forward).
    float Forward { get; }

    // -1 (left) to 1 (right).
    float Strafe { get; }

    // True once per jump: reading it takes it.
    bool TakeJump();
}
