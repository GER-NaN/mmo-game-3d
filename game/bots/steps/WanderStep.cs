namespace MmoGame3d.Bots;

using Godot;
using MmoGame3d.Players;
using MmoGame3d.Zones;

/// <summary>
/// Wanders the zone it is in: walks to a few spots picked at random within its map (or
/// near where it stands, where there is no map), each by a path over the baked mesh, and
/// moves on to the next spot when one is reached or cannot be. What it finds on the way is
/// the watchers' (stuck, out of bounds, floating).
/// </summary>
public class WanderStep : BotStep
{
    // Where there is no map to pick from: this far about the zone's middle.
    private const float NoMapReach = 20f;
    private const double SpotLimit = 25;

    private readonly int _spots;
    private int _walked;
    private double _onThisSpot = SpotLimit;
    private bool _going;

    public WanderStep(int spots)
        : base("wander to " + spots + " spots", (spots * SpotLimit) + DefaultTimeLimit)
    {
        _spots = spots;
    }

    public override BotIntent Intent
    {
        get { return BotIntent.Walking; }
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        Zone? zone = body.Zone;
        Player? player = body.Player;

        if (zone == null || player == null)
        {
            return BotStepState.Running;
        }

        _onThisSpot += delta;
        body.Navigator.Tick(body, delta);

        if (_going && !body.Navigator.Arrived && _onThisSpot < SpotLimit)
        {
            return BotStepState.Running;
        }

        if (_going)
        {
            _walked++;
        }

        if (_walked >= _spots)
        {
            return BotStepState.Done;
        }

        Vector2 half = zone.MapSize == Vector2.Zero ? new Vector2(NoMapReach, NoMapReach) : zone.MapSize / 2f;
        Vector3 spot = new Vector3((float)((body.Random.NextDouble() * 2) - 1) * half.X, 0, (float)((body.Random.NextDouble() * 2) - 1) * half.Y);
        body.Events.Write("wandering", "to " + zone.ZoneId + " (" + spot.X.ToString("0") + ", " + spot.Z.ToString("0") + ")");
        body.Navigator.Go(zone, zone.ToGlobal(spot), false);
        _going = true;
        _onThisSpot = 0;
        return BotStepState.Running;
    }

    public override void End(BotBody body)
    {
        body.Navigator.Stop(body);
    }
}
