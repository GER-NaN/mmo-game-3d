namespace MmoGame3d.BotJudging;

using System.Numerics;

/// <summary>
/// A traveller as its player sees it: not moving, and not in the zone it is going to, for
/// a good while, is stuck. Moving, a new zone, or a ride (a taxi's cabin, where a rider
/// sits still) starts the clock again.
/// </summary>
public sealed class TravelWatch
{
    public const double StillFor = 25;
    public const float Moved = 1.5f;

    private readonly string _to;
    private Vector3 _from;
    private string _zone = "";
    private double _still;
    private bool _started;

    public TravelWatch(string to)
    {
        _to = to;
    }

    // A look after delta seconds; the reason it is stuck, or null.
    public string? Look(double delta, string zone, Vector3 at, bool riding = false)
    {
        if (!_started || riding || zone != _zone || Vector3.Distance(at, _from) > Moved)
        {
            _started = true;
            _zone = zone;
            _from = at;
            _still = 0;
            return null;
        }

        if (zone == _to)
        {
            return null;
        }

        _still += delta;
        return _still >= StillFor ? "not moving for " + (int)StillFor + " s, and still in " + zone + " on the way to " + _to : null;
    }
}
