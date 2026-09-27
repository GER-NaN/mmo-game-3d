namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Players;
using MmoGame3d.Zones;

/// <summary>
/// Judges where a bot is against where it should be, every few seconds, so a soak run
/// finds position bugs by itself. Two checks, each with room to spare, so a bot a little
/// off is never a finding:
///
/// - Footing: what is under its feet. Every zone groups its scenery under named parents,
///   and a player on the ground stands on Ground, Roads, Terrain, Room or Cabin. On
///   anything else (a car, a roof, a tree, a bench) for BadFootingFor, it is somewhere a
///   player should not get to. Nothing under it for that long, it is falling out of
///   the world; well above the surface under it and not jumping, it is floating.
/// - Vehicles: within a car's footprint, the car is driving through it (cars do not
///   collide with players) or it stands inside one.
/// - Progress: where it has been. Within StuckRadius for StuckAfter, most of that time on
///   walking steps, it is stuck (between buildings, in a corner).
///
/// A finding is written once per kind per bot per RepeatAfter: one JSON line in the soak
/// folder's judge/findings.jsonl with the client's side of it (where, on what, doing
/// what, heading where, where it was before), and a picture of the game view.
/// tools/soak-watch/judge_report.py adds the server's side and writes the report.
/// </summary>
public sealed class BotPositionJudge
{
    private const double CheckEvery = 2;

    // A taxi passes through a bot in about a second: checked far more often.
    private const double VehiclesEvery = 0.5;

    // Back and forth in one place: far travelled, little gained, over a few seconds.
    private const double TrackEvery = 0.25;
    private const double TrackWindow = 5;
    private const float ThrashTravelled = 5f;
    private const float ThrashNet = 1f;
    private const double WalkFailedRepeat = 180;
    private const double BadFootingFor = 10;
    private const float StuckRadius = 2.5f;
    private const double StuckAfter = 30;
    private const double WalkingShare = 0.9;
    private const double RepeatAfter = 180;
    private const float RayUp = 0.5f;
    private const float FloatAbove = 0.8f;
    private const float InsideVehicle = 1.6f;
    private const float RayDown = 4f;

    private static readonly HashSet<string> Walkable = new HashSet<string> { "Ground", "Roads", "Terrain", "Room", "Cabin" };

    private readonly string _profile;
    private readonly List<Sample> _history = new List<Sample>();
    private readonly Dictionary<string, double> _reportedAt = new Dictionary<string, double>();
    private double _clock;
    private double _checkIn = CheckEvery;
    private double _vehiclesIn = VehiclesEvery;
    private double _trackIn = TrackEvery;
    private readonly List<Sample> _track = new List<Sample>();
    private string _historyZone = "";
    private string _badFooting = "";
    private double _badFootingFor;

    public BotPositionJudge(string profile)
    {
        _profile = profile;
    }

    public void Tick(BotBody body, double delta, string activity, BotStep? step)
    {
        _clock += delta;
        _checkIn -= delta;
        _vehiclesIn -= delta;
        Player? me = body.Me;

        if (me == null || !me.IsInsideTree())
        {
            _history.Clear();
            _badFootingFor = 0;
            return;
        }

        if (_vehiclesIn <= 0)
        {
            _vehiclesIn = VehiclesEvery;
            JudgeVehicles(body, me, activity, step);
        }

        _trackIn -= delta;

        if (_trackIn <= 0)
        {
            _trackIn = TrackEvery;
            JudgeThrashing(body, me, activity, step);
        }

        if (_checkIn > 0)
        {
            return;
        }

        _checkIn = CheckEvery;
        JudgeFooting(body, me, activity, step);

        // A ride's cabin stands still in its own instance: sitting there is not stuck.
        if (body.ZoneId.StartsWith("taxi"))
        {
            _history.Clear();
            return;
        }

        if (body.ZoneId != _historyZone)
        {
            _history.Clear();
            _historyZone = body.ZoneId;
        }

        _history.Add(new Sample(_clock, me.GlobalPosition, step != null && step.Walks));

        while (_history.Count > 0 && _history[0].Time < _clock - StuckAfter - CheckEvery)
        {
            _history.RemoveAt(0);
        }

        JudgeProgress(body, me, activity, step);
    }

    private void JudgeFooting(BotBody body, Player me, string activity, BotStep? step)
    {
        string under;
        float above;
        Under(body, me, out under, out above);
        string root = under.Split('/')[0];

        // On walkable ground, the feet are on it; well above it and not jumping, the body
        // stands on something the world does not have (or hangs in the air).
        bool floating = under.Length > 0 && above > FloatAbove && !me.IsAirborne;
        bool wrong = under.Length == 0 || !Walkable.Contains(root) || floating;

        if (!wrong)
        {
            _badFooting = "";
            _badFootingFor = 0;
            return;
        }

        _badFootingFor = under == _badFooting ? _badFootingFor + CheckEvery : 0;
        _badFooting = under;

        if (_badFootingFor >= BadFootingFor)
        {
            string kind = under.Length == 0 ? "no-footing" : floating && Walkable.Contains(root) ? "floating" : "footing";
            string detail = under.Length == 0
                ? "nothing under the feet for " + _badFootingFor + " s (falling out of the world?)"
                : (kind == "floating" ? "floating " : "standing on ") + under + " for " + _badFootingFor + " s, " + above.ToString("0.00") + " m above it";
            Report(kind, detail, body, me, activity, step, under, above);
        }
    }

    // Many metres travelled over a few seconds, and barely anywhere gained: the bot
    // jerks back and forth in one place (its own steering, or the server putting it back).
    private void JudgeThrashing(BotBody body, Player me, string activity, BotStep? step)
    {
        _track.Add(new Sample(_clock, me.GlobalPosition, step != null && step.Walks));

        while (_track.Count > 0 && _track[0].Time < _clock - TrackWindow)
        {
            _track.RemoveAt(0);
        }

        if (_track.Count < 2 || _clock - _track[0].Time < TrackWindow - TrackEvery)
        {
            return;
        }

        float travelled = 0f;

        for (int i = 1; i < _track.Count; i++)
        {
            travelled += _track[i].At.DistanceTo(_track[i - 1].At);
        }

        float net = _track[_track.Count - 1].At.DistanceTo(_track[0].At);

        if (travelled > ThrashTravelled && net < ThrashNet)
        {
            string under;
            float above;
            Under(body, me, out under, out above);
            Report("thrashing", "travelled " + travelled.ToString("0.0") + " m in " + TrackWindow + " s and ended " + net.ToString("0.0") + " m from where it began", body, me, activity, step, under, above);
        }
    }

    // A vehicle's body does not collide with players, so it can drive through one, or
    // one can stand inside it: either way the bot is where a car is.
    private void JudgeVehicles(BotBody body, Player me, string activity, BotStep? step)
    {
        Node? vehicles = body.Zone?.GetNodeOrNull("Vehicles");

        if (vehicles == null)
        {
            return;
        }

        foreach (Node node in vehicles.GetChildren())
        {
            Node3D? car = node as Node3D;

            if (car == null)
            {
                continue;
            }

            Vector3 apart = car.GlobalPosition - me.GlobalPosition;
            float flat = new Vector2(apart.X, apart.Z).Length();

            if (flat < InsideVehicle && Mathf.Abs(apart.Y) < 2.5f)
            {
                string under;
                float above;
                Under(body, me, out under, out above);
                Report("in-vehicle", "inside Vehicles/" + car.Name + ", " + flat.ToString("0.0") + " m from its middle, " + (-apart.Y).ToString("0.00") + " m above its base", body, me, activity, step, under, above);
                return;
            }
        }
    }

    // A walk the bot gave up on after its tries at working round something: stuck, if
    // only for a while, and the picture shows where.
    public void WalkFailed(BotBody body, string activity, BotStep step)
    {
        Player? me = body.Me;
        double last;

        if (me == null || (_reportedAt.TryGetValue("walk-failed", out last) && _clock - last < WalkFailedRepeat))
        {
            return;
        }

        string under;
        float above;
        Under(body, me, out under, out above);
        Vector3? target = step.Target(body);
        _reportedAt.Remove("walk-failed");
        Report("walk-failed", "could not get to " + (target == null ? "its target" : "(" + target.Value.X.ToString("0") + ", " + target.Value.Z.ToString("0") + ")") + " in \"" + step.Name + "\"", body, me, activity, step, under, above);
    }

    private void JudgeProgress(BotBody body, Player me, string activity, BotStep? step)
    {
        if (_history.Count == 0 || _clock - _history[0].Time < StuckAfter)
        {
            return;
        }

        int walking = 0;

        foreach (Sample sample in _history)
        {
            if (sample.At.DistanceTo(me.GlobalPosition) > StuckRadius)
            {
                return;
            }

            if (sample.Walking)
            {
                walking++;
            }
        }

        if (walking < _history.Count * WalkingShare)
        {
            return;
        }

        string under;
        float above;
        Under(body, me, out under, out above);
        Report("stuck", "within " + StuckRadius + " m for " + (int)(_clock - _history[0].Time) + " s while walking", body, me, activity, step, under, above);
    }

    // What the ray down from the feet hits, as its path under the zone ("Roads/Main0",
    // "Vehicles/CarTaxi3"), and how far above it the feet are. "" when nothing is there.
    private static void Under(BotBody body, Player me, out string under, out float above)
    {
        under = "";
        above = 0f;
        Zone? zone = body.Zone;

        if (zone == null)
        {
            return;
        }

        Vector3 feet = me.GlobalPosition;
        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(feet + (Vector3.Up * RayUp), feet + (Vector3.Down * RayDown), PhysicsLayers.World);
        query.Exclude = new Godot.Collections.Array<Rid> { me.GetRid() };
        Godot.Collections.Dictionary hit = me.GetWorld3D().DirectSpaceState.IntersectRay(query);

        if (hit.Count == 0)
        {
            return;
        }

        Node? collider = hit["collider"].AsGodotObject() as Node;
        above = feet.Y - ((Vector3)hit["position"]).Y;

        // Up to the zone's own child, which names what it is part of, keeping the one
        // below it, which names the thing.
        Node? group = collider;
        Node? thing = null;

        while (group != null && group.GetParent() != zone)
        {
            thing = group;
            group = group.GetParent();
        }

        if (group == null)
        {
            under = collider == null ? "?" : "?/" + collider.Name;
            return;
        }

        under = group.Name + (thing != null ? "/" + thing.Name : "");
    }

    private void Report(string kind, string detail, BotBody body, Player me, string activity, BotStep? step, string under, float above)
    {
        double last;

        if (_reportedAt.TryGetValue(kind, out last) && _clock - last < RepeatAfter)
        {
            return;
        }

        _reportedAt[kind] = _clock;
        Vector3? target = step?.Target(body);
        List<double[]> recent = new List<double[]>();

        for (int i = Math.Max(0, _history.Count - 8); i < _history.Count; i++)
        {
            Sample sample = _history[i];
            recent.Add(new double[] { Math.Round(_clock - sample.Time), Round(sample.At.X), Round(sample.At.Y), Round(sample.At.Z) });
        }

        BotFindings.Write(me, _profile, kind, detail, new Dictionary<string, object?>
        {
            { "zone", body.ZoneId },
            { "position", new double[] { Round(me.GlobalPosition.X), Round(me.GlobalPosition.Y), Round(me.GlobalPosition.Z) } },
            { "under", under },
            { "above", Math.Round(above, 2) },
            { "hp", me.Health },
            { "airborne", me.IsAirborne },
            { "activity", activity },
            { "step", step?.Name ?? "" },
            { "target", target == null ? null : new double[] { Round(target.Value.X), Round(target.Value.Y), Round(target.Value.Z) } },
            { "to_target", target == null ? null : Math.Round(body.DistanceTo(target.Value), 1) },
            { "recent", recent },
        });
    }

    private static double Round(float value)
    {
        return Math.Round(value, 2);
    }

    private sealed class Sample
    {
        public Sample(double time, Vector3 at, bool walking)
        {
            Time = time;
            At = at;
            Walking = walking;
        }

        public double Time { get; }

        public Vector3 At { get; }

        public bool Walking { get; }
    }
}
