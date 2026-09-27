namespace MmoGame3d.Dev;

using System;
using Godot;
using MmoGame3d.Players;

public enum StepResult
{
    Running,
    Done,
    Failed,
}

/// <summary>
/// One thing a bot does inside an activity: walk somewhere, use something, click a
/// button, wait. Each says when it is done, and each has a time limit; past it the step
/// has failed, and so has its activity. That is every activity's way out.
/// </summary>
public abstract class BotStep
{
    protected BotStep(string name, double limit)
    {
        Name = name;
        Limit = limit;
    }

    public string Name { get; }

    public double Limit { get; }

    // Steps that take the bot to another zone; any other zone change is a surprise.
    public virtual bool MovesZone
    {
        get { return false; }
    }

    // Where the step is taking the bot, if anywhere: for the judge's records.
    public virtual Vector3? Target(BotBody body)
    {
        return null;
    }

    // A step that walks: the judge expects the bot to move during it.
    public virtual bool Walks
    {
        get { return false; }
    }

    // A step that pushes against what stops a player, on purpose (the escaper at the
    // edge, the wedger in a gap): not stuck, not thrashing, while it lasts.
    public virtual bool Presses
    {
        get { return false; }
    }

    public virtual void Begin(BotBody body)
    {
    }

    public abstract StepResult Tick(BotBody body, double delta);
}

/// <summary>A step written in place, for the one-off ones.</summary>
public sealed class DoStep : BotStep
{
    private readonly Func<BotBody, double, StepResult> _tick;
    private readonly bool _movesZone;

    public DoStep(string name, double limit, Func<BotBody, double, StepResult> tick, bool movesZone = false)
        : base(name, limit)
    {
        _tick = tick;
        _movesZone = movesZone;
    }

    public override bool MovesZone
    {
        get { return _movesZone; }
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        return _tick(body, delta);
    }
}

/// <summary>
/// Walking to a point: along the navigation path round the buildings where the zone has
/// one (BotNavigation), straight where it has not, and getting unstuck on the way: not
/// moving for a while, it backs off, turns aside, walks on with a jump, and tries again.
/// After a few tries the point is out of reach.
/// </summary>
public sealed class Walker
{
    private const float Moved = 0.5f;
    private const double StuckAfter = 2.5;
    private const int MaxUnsticks = 8;
    private const double RepathEvery = 3;
    private const float WaypointReached = 0.8f;

    private Vector3 _lastAt;
    private bool _haveLastAt;
    private double _sinceProgress;
    private double _unstickLeft = -1;
    private Vector3[] _path = System.Array.Empty<Vector3>();
    private int _next;
    private Vector3 _pathTo;
    private double _repathIn;
    private double _closeIn;
    private int _unsticks;
    private bool _turnLeft;

    // Each detour is different, turning further and walking longer as tries mount.
    private double _backFor;
    private double _turnFor;
    private double _onFor;

    // Done at the point (within near), Failed when it cannot get there.
    public StepResult Walk(BotBody body, Vector3 target, float near, double delta)
    {
        if (_unstickLeft >= 0)
        {
            Unstick(body, delta);
            _repathIn = 0;
            return StepResult.Running;
        }

        // Still at a terminal (a goal dropped mid-run leaves it open): off it first, as a
        // person would, since no key walks while it is up.
        if (body.CannotWalk)
        {
            _closeIn -= delta;

            if (_closeIn <= 0)
            {
                _closeIn = 1;
                GD.Print("Bot: closing the screen before walking");
                body.CloseOne();
            }

            return StepResult.Running;
        }

        Players.Player? me = body.Me;

        if (me == null)
        {
            return StepResult.Running;
        }

        if (body.DistanceTo(target) <= near)
        {
            body.Stop();
            return StepResult.Done;
        }

        if (body.Navigation.Pending)
        {
            body.Stop();
            _repathIn = 0;
            return StepResult.Running;
        }

        body.SteerTo(Aim(body, me.GlobalPosition, target, delta));

        // Stuck is not moving: a path round a building takes the bot away from the
        // target for a while, so getting no closer is not stuck.
        if (!_haveLastAt || me.GlobalPosition.DistanceTo(_lastAt) > Moved)
        {
            _lastAt = me.GlobalPosition;
            _haveLastAt = true;
            _sinceProgress = 0;
            return StepResult.Running;
        }

        _sinceProgress += delta;

        if (_sinceProgress < StuckAfter)
        {
            return StepResult.Running;
        }

        if (_unsticks >= MaxUnsticks)
        {
            body.Stop();
            return StepResult.Failed;
        }

        _unsticks++;
        _sinceProgress = 0;
        _turnLeft = body.Random.Next(2) == 0;
        _backFor = 0.4 + (body.Random.NextDouble() * 0.6);
        _turnFor = 0.4 + (body.Random.NextDouble() * 0.4 * _unsticks);
        _onFor = 1 + (body.Random.NextDouble() * 0.5 * _unsticks);
        _unstickLeft = _backFor + _turnFor + _onFor;
        GD.Print("Bot: stuck, working round it");
        return StepResult.Running;
    }

    // The next point of the path to the target, asked for again now and then, or the
    // target itself with no path.
    private Vector3 Aim(BotBody body, Vector3 at, Vector3 target, double delta)
    {
        _repathIn -= delta;

        if (_repathIn <= 0 || _pathTo.DistanceTo(target) > 1f)
        {
            _path = body.Navigation.Path(at, target);
            _next = 1;
            _pathTo = target;
            _repathIn = RepathEvery;

            // Straight at the target from here on: worth a line in the log, for a finding.
            // A door is carved out of the mesh 2.5 m round, so a path to one ends short.
            float shortBy = _path.Length == 0 ? float.MaxValue : Flat(_path[_path.Length - 1], target);

            if (body.Navigation.Ready && shortBy > 3f)
            {
                GD.Print("Bot: " + (_path.Length == 0 ? "no path" : "the path ends " + shortBy.ToString("0.0") + " m short") + " from ("
                    + at.X.ToString("0") + ", " + at.Z.ToString("0") + ") to (" + target.X.ToString("0") + ", " + target.Z.ToString("0") + "); straight on");
            }
        }

        while (_next < _path.Length - 1 && Flat(at, _path[_next]) < WaypointReached)
        {
            _next++;
        }

        return _next < _path.Length - 1 ? _path[_next] : target;
    }

    private static float Flat(Vector3 a, Vector3 b)
    {
        return new Vector2(a.X - b.X, a.Z - b.Z).Length();
    }

    // Step aside, turn, walk on with a jump. Sideways, not back: backing blind walked
    // bots into the door they had just come out of.
    private void Unstick(BotBody body, double delta)
    {
        body.Stop();
        Input.ActionRelease("strafe_left");
        Input.ActionRelease("strafe_right");
        _unstickLeft -= delta;

        if (_unstickLeft > _turnFor + _onFor)
        {
            Input.ActionPress(_turnLeft ? "strafe_left" : "strafe_right");
        }
        else if (_unstickLeft > _onFor)
        {
            Input.ActionPress(_turnLeft ? "turn_left" : "turn_right");
        }
        else if (_unstickLeft > 0)
        {
            Input.ActionPress("move_forward");
            Input.ActionPress("jump");
        }
        else
        {
            _unstickLeft = -1;
        }
    }
}

/// <summary>Walks to a thing in the zone (by its path) until it is near.</summary>
public sealed class WalkToStep : BotStep
{
    private readonly Func<BotBody, Node3D?> _target;
    private readonly float _near;
    private Walker _walker = new Walker();

    public WalkToStep(string name, Func<BotBody, Node3D?> target, float near = 1.6f, double limit = 60)
        : base(name, limit)
    {
        _target = target;
        _near = near;
    }

    public override bool Walks
    {
        get { return true; }
    }

    public override Vector3? Target(BotBody body)
    {
        Node3D? target = _target(body);
        return target == null ? null : target.GlobalPosition;
    }

    public override void Begin(BotBody body)
    {
        _walker = new Walker();
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        Node3D? target = _target(body);

        if (target == null || !GodotObject.IsInstanceValid(target) || !target.IsInsideTree())
        {
            return StepResult.Failed;
        }

        return _walker.Walk(body, target.GlobalPosition, _near, delta);
    }
}

/// <summary>Walks into a door until the zone changes.</summary>
public sealed class DoorStep : BotStep
{
    private readonly string _door;
    private string _from = "";
    private bool _atFront;
    private Walker _walker = new Walker();

    public DoorStep(string door, double limit = 90)
        : base("go through " + door, limit)
    {
        _door = door;
    }

    public override bool MovesZone
    {
        get { return true; }
    }

    public override bool Walks
    {
        get { return true; }
    }

    public override Vector3? Target(BotBody body)
    {
        Node3D? door = body.Thing("Doors/" + _door);
        return door == null ? null : door.GlobalPosition;
    }

    public override void Begin(BotBody body)
    {
        _from = body.ZoneId;
        _walker = new Walker();
        _atFront = false;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        if (body.ZoneId != _from && body.ZoneId.Length > 0)
        {
            body.Stop();

            // Door ToShop leads to shop: somewhere else is the party pulling it through
            // another door on the way, and the rest of the plan is for the shop.
            string meant = _door.StartsWith("To") ? _door.Substring(2).ToLowerInvariant() : body.ZoneId;

            if (body.ZoneId != meant)
            {
                GD.Print("Bot: arrived in " + body.ZoneId + ", not " + meant);
                return StepResult.Failed;
            }

            return StepResult.Done;
        }

        Node3D? door = body.Thing("Doors/" + _door);

        if (door == null)
        {
            // Between zones for a moment: the old one is gone, the new not yet here.
            return body.Zone == null ? StepResult.Running : StepResult.Failed;
        }

        // First to the spot in front of the door, where players arrive coming out of it
        // (door ToShop, marker FromShop): straight at the door from the wrong side is
        // straight into its building.
        Node3D? front = _door.StartsWith("To") ? body.Thing("Arrivals/From" + _door.Substring(2)) : null;

        if (front != null && !_atFront)
        {
            StepResult there = _walker.Walk(body, front.GlobalPosition, 1f, delta);

            if (there == StepResult.Done)
            {
                _atFront = true;
                _walker = new Walker();
            }

            return there == StepResult.Failed ? StepResult.Failed : StepResult.Running;
        }

        // Near is below zero: it keeps walking into the door until the zone changes.
        return _walker.Walk(body, door.GlobalPosition, -1f, delta);
    }
}

/// <summary>
/// Uses the thing in reach: taps F while the prompt says what it expects, until the
/// result shows (a panel opens, the zone changes, the prompt changes).
/// </summary>
public sealed class UseStep : BotStep
{
    private const double Retry = 2;

    private readonly string _prompt;
    private readonly Func<BotBody, bool> _until;
    private readonly bool _movesZone;
    private readonly bool _once;
    private double _nextTap;
    private double _sinceTap = -1;

    // once: one tap, done a moment later, for a use whose result the bot cannot see.
    public UseStep(string prompt, Func<BotBody, bool> until, double limit = 8, bool movesZone = false, bool once = false)
        : base("use \"" + prompt + "\"", limit)
    {
        _prompt = prompt;
        _until = until;
        _movesZone = movesZone;
        _once = once;
    }

    public override bool MovesZone
    {
        get { return _movesZone; }
    }

    public override void Begin(BotBody body)
    {
        _nextTap = 0;
        _sinceTap = -1;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        if (_until(body))
        {
            return StepResult.Done;
        }

        if (_sinceTap >= 0)
        {
            _sinceTap += delta;

            if (_once && _sinceTap > 1.5)
            {
                return StepResult.Done;
            }
        }

        _nextTap -= delta;

        if (_nextTap <= 0 && body.Prompt.Contains(_prompt) && !(_once && _sinceTap >= 0))
        {
            GD.Print("Bot: pressing F at \"" + body.Prompt + "\"");
            body.Interact();
            _nextTap = Retry;
            _sinceTap = 0;
        }

        return StepResult.Running;
    }
}

/// <summary>Wanders for a while: walks, turns, jumps, stands.</summary>
public sealed class WanderStep : BotStep
{
    private readonly double _seconds;
    private double _left;
    private double _spell;
    private Vector3 _spellFrom;
    private bool _spellWalks;

    public WanderStep(double seconds)
        : base("wander", seconds + 5)
    {
        _seconds = seconds;
    }

    public override bool Walks
    {
        get { return true; }
    }

    public override void Begin(BotBody body)
    {
        _left = _seconds;
        _spell = 0;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        _left -= delta;

        if (_left <= 0)
        {
            body.Stop();
            return StepResult.Done;
        }

        _spell -= delta;

        // A door ahead: turn from it, as a person idling about would, rather than
        // leave the zone by chance.
        if (body.DoorAhead(3f))
        {
            body.Stop();
            Input.ActionPress("turn_left");
            _spell = 0.3;
            return StepResult.Running;
        }

        if (_spell > 0)
        {
            return StepResult.Running;
        }

        _spell = 1 + (body.Random.NextDouble() * 3);
        body.Stop();
        Player? me = body.Me;

        // A walk that got nowhere (a parked car, a wall): turn away first, as a person
        // would, not into it again.
        bool blocked = me != null && _spellWalks && me.GlobalPosition.DistanceTo(_spellFrom) < 0.5f;
        _spellFrom = me?.GlobalPosition ?? Vector3.Zero;
        _spellWalks = !blocked;

        if (blocked)
        {
            _spell = 0.5 + (body.Random.NextDouble() * 1.0);
            Input.ActionPress(body.Random.Next(2) == 0 ? "turn_left" : "turn_right");
            return StepResult.Running;
        }

        switch (body.Random.Next(10))
        {
            case 0:
                _spellWalks = false;
                break;
            case 1:
            case 2:
                Input.ActionPress("move_forward");
                Input.ActionPress(body.Random.Next(2) == 0 ? "turn_left" : "turn_right");
                break;
            case 3:
                Input.ActionPress("move_forward");
                Input.ActionPress("jump");
                break;
            default:
                Input.ActionPress("move_forward");
                break;
        }

        return StepResult.Running;
    }
}

/// <summary>Stands still for a moment, as a person does to read.</summary>
public sealed class PauseStep : BotStep
{
    private readonly double _seconds;
    private double _left;

    public PauseStep(double seconds)
        : base("pause", (seconds * 4) + 1)
    {
        _seconds = seconds;
    }

    public override void Begin(BotBody body)
    {
        _left = _seconds * body.Pace;
        body.Stop();
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        _left -= delta;
        return _left <= 0 ? StepResult.Done : StepResult.Running;
    }
}

/// <summary>Closes whatever is open, one thing a moment, until nothing is.</summary>
public sealed class CloseAllStep : BotStep
{
    private const double Gap = 0.5;
    private double _next;

    public CloseAllStep()
        : base("close everything", 10)
    {
    }

    public override void Begin(BotBody body)
    {
        _next = 0;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        _next -= delta;

        if (_next > 0)
        {
            return StepResult.Running;
        }

        _next = Gap;
        return body.CloseOne() ? StepResult.Done : StepResult.Running;
    }
}

/// <summary>
/// Out of a trap (a gap between buildings, a corner): tries eight directions, straight
/// back first, then the sides, a few seconds each with a jump, until one takes the bot
/// well clear of where it was. Walking at a target keeps pushing it deeper in.
/// </summary>
public sealed class EscapeStep : BotStep
{
    private const double TryFor = 2.5;
    private const float Clear = 2.5f;

    // Turns from the way it faces, in the order tried.
    private static readonly float[] Turns = { Mathf.Pi, Mathf.Pi / 2f, -Mathf.Pi / 2f, Mathf.Pi * 0.75f, -Mathf.Pi * 0.75f, Mathf.Pi / 4f, -Mathf.Pi / 4f, 0f };

    private int _try;
    private double _left;
    private Vector3 _from;
    private Vector3 _toward;

    public EscapeStep()
        : base("get unstuck", (TryFor * 8) + 2)
    {
    }

    public override bool Walks
    {
        get { return true; }
    }

    public override void Begin(BotBody body)
    {
        _try = 0;
        Aim(body);
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        Players.Player? me = body.Me;

        if (me == null)
        {
            return StepResult.Failed;
        }

        if (me.GlobalPosition.DistanceTo(_from) > Clear)
        {
            body.Stop();
            GD.Print("Bot: got clear, going " + (int)Mathf.RadToDeg(Turns[_try]) + " degrees from where it faced");
            return StepResult.Done;
        }

        body.SteerTo(_toward);
        Input.ActionPress("jump");
        _left -= delta;

        if (_left <= 0)
        {
            _try++;

            if (_try >= Turns.Length)
            {
                body.Stop();
                return StepResult.Failed;
            }

            Aim(body);
        }

        return StepResult.Running;
    }

    private void Aim(BotBody body)
    {
        Players.Player? me = body.Me;

        if (me == null)
        {
            return;
        }

        _from = me.GlobalPosition;
        _left = TryFor;
        float heading = me.Heading + Turns[_try];
        _toward = _from + (new Vector3(-Mathf.Sin(heading), 0f, -Mathf.Cos(heading)) * 10f);
    }
}
