namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.BotJudging;
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
/// - Progress: where it has been. Within Stuck.Radius for Stuck.After, most of that time on
///   walking steps, it is stuck (between buildings, in a corner).
///
/// A finding is written once per kind per bot per RepeatAfter: one JSON line in the soak
/// folder's judge/findings.jsonl with the client's side of it (where, on what, doing
/// what, heading where, where it was before), and a picture of the game view.
/// tools/bot-watch/report.py adds the server's side and writes the report.
/// </summary>
public sealed class BotPositionJudge
{
    private const double CheckEvery = 2;

    // A taxi passes through a bot in about a second: checked far more often.
    private const double VehiclesEvery = 0.5;

    // The thrashing and stuck checks themselves are plain C# (MmoGame3d.BotJudging), so
    // unit tests hand them any track they like; this class looks and reports.
    private const double TrackEvery = Thrashing.Every;
    private const double TrackWindow = Thrashing.Window;
    private const double WalkFailedRepeat = 60;
    private const double BadFootingFor = 10;
    private const double RepeatAfter = 60;
    private const float RayUp = 0.5f;
    private const float FloatAbove = 0.8f;
    private const float InsideVehicle = 1.6f;

    // Feet this far above the zone's highest arrival or spawn marker: up on something
    // (a roof) even where that something counts as walkable (a taxi cabin's roof is part
    // of the cabin). Not on sculpted ground, which rises far above its markers.
    private const float AboveMarkers = 1.2f;

    // Past the edge of the zone's map by this much, or this far below the zone.
    private const float PastEdge = 5f;
    private const float BelowZone = 10f;
    private const float RayDown = 4f;

    private static readonly HashSet<string> Walkable = new HashSet<string> { "Ground", "Roads", "Terrain", "Room", "Cabin" };

    private readonly string _profile;
    private readonly List<TrackSample> _history = new List<TrackSample>();
    private readonly Dictionary<string, double> _reportedAt = new Dictionary<string, double>();
    private double _clock;
    private double _checkIn = CheckEvery;
    private double _vehiclesIn = VehiclesEvery;
    private double _trackIn = TrackEvery;
    private readonly List<TrackSample> _track = new List<TrackSample>();
    private string _trackZone = "";

    // The thrashing check's samples, for its finding: seconds ago, x, z, how far apart
    // the client and the server have the body, and the movement keys held.
    private List<object[]>? _thrashTrack;

    // Further than this between two quarter-second samples is not walking.
    private const float Teleport = 4f;
    private string _historyZone = "";
    private string _badFooting = "";
    private double _badFootingFor;

    // Standing in a door's trigger with no zone change: the door fires only on the way
    // in, so a touch it dropped leaves the player in the doorway.
    private const double DoorIgnoredAfter = 6;
    private string _inDoor = "";
    private string _inDoorZone = "";
    private double _inDoorFor;

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

        // Pushing on purpose: what comes after (walking away) is what counts.
        if (step != null && step.Presses)
        {
            _track.Clear();
            _history.Clear();
            IsStuck = false;
        }

        _trackIn -= delta;

        if (_trackIn <= 0 && (step == null || !step.Presses))
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
        JudgeDoorway(body, me, activity, step);
        JudgeBounds(body, me, activity, step);

        // Online (a terminal, the phone) or reading a panel, standing still is the point:
        // not stuck, and the clock starts again after. Unless the step is a walk: then the
        // bot means to go and cannot, which is just what stuck is.
        if ((body.IsOnline || body.OpenPanels().Count > 0) && (step == null || !step.Walks))
        {
            _history.Clear();
            IsStuck = false;
            return;
        }

        // A ride's cabin stands still in its own instance: sitting there is not stuck.
        if (body.ZoneId.StartsWith("taxi") || (step != null && step.Presses))
        {
            _history.Clear();
            return;
        }

        if (body.ZoneId != _historyZone)
        {
            _history.Clear();
            _historyZone = body.ZoneId;
        }

        _history.Add(new TrackSample(_clock, Numeric(me.GlobalPosition), step != null && step.Walks));

        while (_history.Count > 0 && _history[0].Time < _clock - Stuck.After - CheckEvery)
        {
            _history.RemoveAt(0);
        }

        JudgeProgress(body, me, activity, step);
    }

    private void JudgeDoorway(BotBody body, Player me, string activity, BotStep? step)
    {
        string door = body.DoorIn();
        _inDoorFor = door.Length > 0 && door == _inDoor && body.ZoneId == _inDoorZone ? _inDoorFor + CheckEvery : 0;
        _inDoor = door;
        _inDoorZone = body.ZoneId;

        if (_inDoorFor >= DoorIgnoredAfter && _inDoorFor - CheckEvery < DoorIgnoredAfter)
        {
            string under;
            float above;
            Under(body, me, out under, out above);
            Report("door-ignored", "standing in the trigger of " + door + " for " + (int)_inDoorFor + " s and still in " + body.ZoneId, body, me, activity, step, under, above);
        }
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
        bool tooHigh = root != "Terrain" && !me.IsAirborne && me.GlobalPosition.Y > HighestMarker(body) + AboveMarkers;
        bool wrong = under.Length == 0 || !Walkable.Contains(root) || floating || tooHigh;

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
            string kind = under.Length == 0 ? "no-footing" : !Walkable.Contains(root) ? "footing" : floating ? "floating" : "too-high";
            string detail = under.Length == 0
                ? "nothing under the feet for " + _badFootingFor + " s (falling out of the world?)"
                : kind == "too-high"
                    ? "standing on " + under + ", " + (me.GlobalPosition.Y - HighestMarker(body)).ToString("0.0") + " m above the zone's markers, for " + _badFootingFor + " s"
                    : (kind == "floating" ? "floating " : "standing on ") + under + " for " + _badFootingFor + " s, " + above.ToString("0.00") + " m above it";
            Report(kind, detail, body, me, activity, step, under, above);
        }
    }

    // Sharp reversals over a few seconds, and barely anywhere gained: the bot jerks back
    // and forth in one place (its own steering, or the server putting it back).
    private void JudgeThrashing(BotBody body, Player me, string activity, BotStep? step)
    {
        // A new zone, or a jump no walk makes (a door, a respawn, the party pulling it
        // along): the track starts again, or the jump would count as travel.
        if (body.ZoneId != _trackZone || (_track.Count > 0 && System.Numerics.Vector3.Distance(Numeric(me.GlobalPosition), _track[_track.Count - 1].At) > Teleport))
        {
            _track.Clear();
            _trackZone = body.ZoneId;
        }

        _track.Add(new TrackSample(_clock, Numeric(me.GlobalPosition), step != null && step.Walks) { Gap = me.Position.DistanceTo(me.NetPosition), Keys = HeldKeys() });

        while (_track.Count > 0 && _track[0].Time < _clock - TrackWindow)
        {
            _track.RemoveAt(0);
        }

        ThrashVerdict? verdict = Thrashing.Judge(_track);

        if (verdict == null)
        {
            return;
        }

        string under;
        float above;
        Under(body, me, out under, out above);

        // Whether the server put the body back (a wide gap between where the client and
        // the server have it) or the bot's own keys flipped: the track tells.
        List<object[]> track = new List<object[]>();

        foreach (TrackSample sample in _track)
        {
            track.Add(new object[] { Math.Round(_clock - sample.Time, 2), Round(sample.At.X), Round(sample.At.Z), Math.Round(sample.Gap, 2), sample.Keys });
        }

        _thrashTrack = track;
        Report("thrashing", verdict.Reversals + " sharp reversals in " + TrackWindow + " s, " + verdict.Travelled.ToString("0.0") + " m travelled, " + verdict.Net.ToString("0.0")
            + " m gained; client and server up to " + verdict.WidestGap.ToString("0.0") + " m apart", body, me, activity, step, under, above);
        _thrashTrack = null;
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

    // Outside the zone's map, or far below it: out of the world a player should be in.
    private void JudgeBounds(BotBody body, Player me, string activity, BotStep? step)
    {
        Zones.Zone? zone = body.Zone;

        if (zone == null)
        {
            return;
        }

        Vector3 local = zone.ToLocal(me.GlobalPosition);
        bool past = zone.MapSize != Vector2.Zero
            && (Mathf.Abs(local.X) > (zone.MapSize.X / 2f) + PastEdge || Mathf.Abs(local.Z) > (zone.MapSize.Y / 2f) + PastEdge);

        if (!past && local.Y > -BelowZone)
        {
            return;
        }

        string under;
        float above;
        Under(body, me, out under, out above);
        string where = past
            ? "past the map's edge at (" + local.X.ToString("0") + ", " + local.Z.ToString("0") + "), the map being " + zone.MapSize.X.ToString("0") + " by " + zone.MapSize.Y.ToString("0") + " m"
            : local.Y.ToString("0.0") + " m below the zone";
        Report("out-of-bounds", where, body, me, activity, step, under, above);
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

    // The highest of the zone's arrival and spawn markers, in world space: the level its
    // players are meant to stand at.
    private static float HighestMarker(BotBody body)
    {
        Zone? zone = body.Zone;
        float highest = float.MinValue;

        if (zone == null)
        {
            return 0f;
        }

        Node3D? spawn = zone.GetNodeOrNull<Node3D>("Spawn");

        if (spawn != null)
        {
            highest = spawn.GlobalPosition.Y;
        }

        Node? arrivals = zone.GetNodeOrNull("Arrivals");

        if (arrivals != null)
        {
            foreach (Node node in arrivals.GetChildren())
            {
                Node3D? marker = node as Node3D;

                if (marker != null)
                {
                    highest = Mathf.Max(highest, marker.GlobalPosition.Y);
                }
            }
        }

        return highest == float.MinValue ? zone.GlobalPosition.Y : highest;
    }

    // Stuck by the judge's own measure, now: the bot acts on it (BotDriver escapes).
    public bool IsStuck { get; private set; }

    // Starts the stuck clock again: after an escape, the old spot no longer counts.
    public void Forget()
    {
        _history.Clear();
        IsStuck = false;
    }

    private void JudgeProgress(BotBody body, Player me, string activity, BotStep? step)
    {
        IsStuck = false;

        if (!Stuck.Judge(_history, Numeric(me.GlobalPosition), _clock))
        {
            return;
        }

        IsStuck = true;
        string under;
        float above;
        Under(body, me, out under, out above);
        Report("stuck", "within " + Stuck.Radius + " m for " + (int)(_clock - _history[0].Time) + " s while walking", body, me, activity, step, under, above);
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
            TrackSample sample = _history[i];
            recent.Add(new double[] { Math.Round(_clock - sample.Time), Round(sample.At.X), Round(sample.At.Y), Round(sample.At.Z) });
        }

        BotFindings.Write(me, _profile, kind, detail, new Dictionary<string, object?>
        {
            { "zone", body.ZoneId },
            { "body_age", Math.Round(body.BodyAge, 1) },
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
            { "track", _thrashTrack },
        });
    }

    private static string HeldKeys()
    {
        string[] keys = { "move_forward", "move_back", "turn_left", "turn_right", "strafe_left", "strafe_right", "jump" };
        List<string> held = new List<string>();

        foreach (string key in keys)
        {
            if (Input.IsActionPressed(key))
            {
                held.Add(key.Replace("move_", "").Replace("strafe_", "step_"));
            }
        }

        return string.Join("+", held);
    }

    private static double Round(float value)
    {
        return Math.Round(value, 2);
    }

    private static System.Numerics.Vector3 Numeric(Vector3 v)
    {
        return new System.Numerics.Vector3(v.X, v.Y, v.Z);
    }
}
