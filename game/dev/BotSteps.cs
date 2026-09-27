namespace MmoGame3d.Dev;

using System;
using Godot;

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
/// Walking to a point, and getting unstuck on the way: with no progress for a while it
/// backs off, turns aside, walks on with a jump, and tries again. After a few tries
/// the point is out of reach.
/// </summary>
public sealed class Walker
{
    private const float Progress = 0.5f;
    private const double StuckAfter = 2.5;
    private const int MaxUnsticks = 8;

    private float _best = float.MaxValue;
    private double _sinceProgress;
    private double _unstickLeft = -1;
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
            return StepResult.Running;
        }

        float distance = body.SteerTo(target);

        if (distance <= near)
        {
            body.Stop();
            return StepResult.Done;
        }

        if (distance < _best - Progress)
        {
            _best = distance;
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
        _best = float.MaxValue;
        _turnLeft = body.Random.Next(2) == 0;
        _backFor = 0.4 + (body.Random.NextDouble() * 0.6);
        _turnFor = 0.4 + (body.Random.NextDouble() * 0.4 * _unsticks);
        _onFor = 1 + (body.Random.NextDouble() * 0.5 * _unsticks);
        _unstickLeft = _backFor + _turnFor + _onFor;
        GD.Print("Bot: stuck, working round it");
        return StepResult.Running;
    }

    // Back off, turn aside, walk on with a jump.
    private void Unstick(BotBody body, double delta)
    {
        body.Stop();
        _unstickLeft -= delta;

        if (_unstickLeft > _turnFor + _onFor)
        {
            Input.ActionPress("move_back");
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

    public WanderStep(double seconds)
        : base("wander", seconds + 5)
    {
        _seconds = seconds;
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

        if (_spell > 0)
        {
            return StepResult.Running;
        }

        _spell = 1 + (body.Random.NextDouble() * 3);
        body.Stop();

        switch (body.Random.Next(10))
        {
            case 0:
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
        : base("pause", seconds + 1)
    {
        _seconds = seconds;
    }

    public override void Begin(BotBody body)
    {
        _left = _seconds;
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
