namespace MmoGame3d.Dev;

using Godot;
using MmoGame3d.BotJudging;
using MmoGame3d.Players;
using MmoGame3d.Rules.World;

/// <summary>
/// A traveller as its player sees it: not moving, and not there yet, for a good while, is
/// stuck (TravelWatch decides; this looks).
/// </summary>
public sealed class TravelJudge : BotActivityJudge
{
    private readonly TravelWatch _watch;

    public TravelJudge(string to)
    {
        _watch = new TravelWatch(to);
    }

    public override string? Watch(BotBody body, double delta)
    {
        Player? me = body.Me;

        if (me == null)
        {
            return null;
        }

        Vector3 at = me.GlobalPosition;
        return _watch.Look(delta, body.ZoneId, new System.Numerics.Vector3(at.X, at.Y, at.Z), ZoneIds.IsInstance(body.ZoneId));
    }
}
