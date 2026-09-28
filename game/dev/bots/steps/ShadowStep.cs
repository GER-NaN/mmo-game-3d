namespace MmoGame3d.Dev;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Players;

/// <summary>
/// Follows another player at arm's length and uses what they use, through doors too:
/// two players on one terminal, one shopkeeper, one door at the same moment, arriving on
/// the same spot.
/// </summary>
public sealed class ShadowStep : BotStep
{
    private const float Close = 1.5f;
    private const float DoorReach = 5f;

    private readonly double _seconds;
    private double _left;
    private string _name = "";
    private Player? _other;
    private Vector3 _lastSeen;
    private DoorStep? _door;
    private Walker _walker = new Walker();
    private double _useIn;
    private double _closeIn = -1;
    private bool _walking;

    public ShadowStep(double seconds)
        : base("shadow someone", seconds + 10)
    {
        _seconds = seconds;
    }

    // Only while closing in or following: at the elbow of someone standing still, the
    // shadow stands still too.
    public override bool Walks
    {
        get { return _walking; }
    }

    public override bool MovesZone
    {
        get { return true; }
    }

    public override Vector3? Target(BotBody body)
    {
        return _other != null && GodotObject.IsInstanceValid(_other) ? _other.GlobalPosition : null;
    }

    public override void Begin(BotBody body)
    {
        _left = _seconds;
        _walker = new Walker();
        _door = null;
        _useIn = 2;
        _closeIn = -1;
        _other = Pick(body, "");
        _name = _other?.DisplayName ?? "";

        if (_other != null)
        {
            GD.Print("Bot: shadowing " + _name);
        }
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        _left -= delta;
        _walking = false;

        if (_left <= 0 || body.Me == null)
        {
            body.Stop();
            return _name.Length > 0 ? StepResult.Done : StepResult.Failed;
        }

        // A panel the last use opened: a moment in it, then away, as the other goes on.
        if (_closeIn >= 0)
        {
            _closeIn -= delta;

            if (_closeIn < 0)
            {
                body.CloseOne();
            }

            return StepResult.Running;
        }

        // After them through the door they took, as any door is taken (DoorStep); then
        // find them on the other side.
        if (_door != null)
        {
            _walking = true;
            StepResult through = _door.Tick(body, delta);

            if (through == StepResult.Running)
            {
                return StepResult.Running;
            }

            _door = null;
            _walker = new Walker();
            _other = through == StepResult.Done ? Pick(body, _name) : null;
            return _other == null ? StepResult.Done : StepResult.Running;
        }

        if (_other == null || !GodotObject.IsInstanceValid(_other) || !_other.IsInsideTree())
        {
            Node3D? door = NearestDoor(body, _lastSeen);

            if (door == null)
            {
                return _name.Length > 0 ? StepResult.Done : StepResult.Failed;
            }

            GD.Print("Bot: following " + _name + " through " + door.Name);
            _door = new DoorStep(door.Name);
            _door.Begin(body);
            _other = null;
            return StepResult.Running;
        }

        _lastSeen = _other.GlobalPosition;
        StepResult walked = _walker.Walk(body, _lastSeen, Close, delta);

        if (walked == StepResult.Failed)
        {
            _walker = new Walker();
        }

        if (walked != StepResult.Done)
        {
            _walking = true;
            return StepResult.Running;
        }

        // At their elbow: use whatever they are next to, now and then.
        _useIn -= delta;

        if (_useIn <= 0 && body.Prompt.Length > 0)
        {
            _useIn = 3 + (body.Random.NextDouble() * 4);
            GD.Print("Bot: using what " + _name + " is at: " + body.Prompt);
            body.Interact();
            _closeIn = 2 + body.Random.NextDouble() * 3;
        }

        return StepResult.Running;
    }

    // Another player in this zone: the one named, or anyone.
    private static Player? Pick(BotBody body, string name)
    {
        Player? me = body.Me;

        if (me == null)
        {
            return null;
        }

        List<Player> others = new List<Player>();

        foreach (Node node in me.GetParent().GetChildren())
        {
            Player? other = node as Player;

            if (other != null && other != me && (name.Length == 0 || other.DisplayName == name))
            {
                others.Add(other);
            }
        }

        return others.Count == 0 ? null : others[body.Random.Next(others.Count)];
    }

    private static Node3D? NearestDoor(BotBody body, Vector3 at)
    {
        Node? doors = body.Zone?.GetNodeOrNull("Doors");
        Node3D? nearest = null;

        foreach (Node node in doors?.GetChildren() ?? new Godot.Collections.Array<Node>())
        {
            Node3D? door = node as Node3D;

            if (door != null && door.GlobalPosition.DistanceTo(at) < DoorReach && (nearest == null || door.GlobalPosition.DistanceTo(at) < nearest.GlobalPosition.DistanceTo(at)))
            {
                nearest = door;
            }
        }

        return nearest;
    }
}
