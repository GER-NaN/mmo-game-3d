namespace MmoGame3d.Bots;

using Godot;
using MmoGame3d.Players;

/// <summary>
/// Off the ground and not falling, for seconds: hung on something's edge, or held in the
/// air. The body's own sense of the floor: the client moves it with the same collision as
/// the server.
/// </summary>
public class FloatingWatcher : BotWatcher
{
    private const double Seconds = 5;
    private const float StillVertical = 0.3f;

    private double _for;

    public FloatingWatcher()
        : base("floating", true)
    {
    }

    public override string? Look(BotBody body, BotStep? step, double delta)
    {
        Player? player = body.Player;

        if (player == null || player.IsOnFloor() || Mathf.Abs(player.Velocity.Y) > StillVertical)
        {
            _for = 0;
            return null;
        }

        _for += delta;
        return _for >= Seconds ? "off the ground and not falling for " + Seconds + " s" : null;
    }
}
