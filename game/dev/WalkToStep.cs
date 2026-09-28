namespace MmoGame3d.Dev;

using System;
using Godot;

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
