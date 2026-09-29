namespace MmoGame3d.Bots;

using Godot;
using MmoGame3d.Players;
using MmoGame3d.Zones;

/// <summary>
/// Somewhere a player should never be: past the zone's map by a few metres, or well
/// below its ground (fallen through the world).
/// </summary>
public class OutOfBoundsWatcher : BotWatcher
{
    private const float PastTheMap = 5f;
    private const float BelowGround = -10f;

    public OutOfBoundsWatcher()
        : base("out-of-bounds", true)
    {
    }

    public override string? Look(BotBody body, BotStep? step, double delta)
    {
        Player? player = body.Player;
        Zone? zone = body.Zone;

        if (player == null || zone == null)
        {
            return null;
        }

        Vector3 at = zone.ToLocal(player.GlobalPosition);

        if (at.Y < BelowGround)
        {
            return "fell below the ground of " + zone.ZoneId;
        }

        if (zone.MapSize == Vector2.Zero)
        {
            return null;
        }

        bool past = Mathf.Abs(at.X) > (zone.MapSize.X / 2f) + PastTheMap || Mathf.Abs(at.Z) > (zone.MapSize.Y / 2f) + PastTheMap;
        return past ? "past the edge of " + zone.ZoneId + "'s map" : null;
    }
}
