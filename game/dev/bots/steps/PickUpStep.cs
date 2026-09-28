namespace MmoGame3d.Dev;

using Godot;

/// <summary>Walks over the things lying about (walking over one picks it up), up to three.</summary>
public sealed class PickUpStep : BotStep
{
    private Walker _walker = new Walker();
    private Node3D? _item;
    private int _taken;

    public PickUpStep()
        : base("pick things up", 90)
    {
    }

    public override bool Walks
    {
        get { return true; }
    }

    public override Vector3? Target(BotBody body)
    {
        return _item != null && GodotObject.IsInstanceValid(_item) && _item.IsInsideTree() ? _item.GlobalPosition : null;
    }

    public override void Begin(BotBody body)
    {
        _item = null;
        _taken = 0;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        if (_item == null || !GodotObject.IsInstanceValid(_item) || !_item.IsInsideTree())
        {
            if (_item != null)
            {
                _taken++;
            }

            _item = body.Nearest(body.GroundItems());
            _walker = new Walker();

            if (_item == null || _taken >= 3)
            {
                body.Stop();
                return _taken > 0 ? StepResult.Done : StepResult.Failed;
            }
        }

        // Near is below zero: it walks onto the thing until it is picked up and gone.
        if (_walker.Walk(body, _item.GlobalPosition, -1f, delta) == StepResult.Failed)
        {
            body.Unreachable.Add(_item.Name);
            return StepResult.Failed;
        }

        return StepResult.Running;
    }
}
