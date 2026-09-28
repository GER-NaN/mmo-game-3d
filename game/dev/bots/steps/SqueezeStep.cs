namespace MmoGame3d.Dev;

using System.Collections.Generic;
using Godot;

/// <summary>
/// Walks straight into the gap between a building and its nearest neighbour and keeps
/// pushing a while: where a player can get wedged, the judges report it.
/// </summary>
public sealed class SqueezeStep : BotStep
{
    private const double PushFor = 20;

    private Vector3? _gap;
    private double _left;

    public SqueezeStep()
        : base("squeeze into a gap", PushFor + 10)
    {
    }

    public override bool Walks
    {
        get { return true; }
    }

    public override bool Presses
    {
        get { return true; }
    }

    public override Vector3? Target(BotBody body)
    {
        return _gap;
    }

    public override void Begin(BotBody body)
    {
        _left = PushFor;
        _gap = null;
        Node? buildings = body.Zone?.GetNodeOrNull("Buildings");

        if (buildings == null || buildings.GetChildCount() < 2)
        {
            return;
        }

        List<Node3D> all = new List<Node3D>();

        foreach (Node node in buildings.GetChildren())
        {
            Node3D? building = node as Node3D;

            if (building != null)
            {
                all.Add(building);
            }
        }

        Node3D first = all[body.Random.Next(all.Count)];
        Node3D? nearest = null;

        foreach (Node3D other in all)
        {
            if (other != first && (nearest == null || other.GlobalPosition.DistanceTo(first.GlobalPosition) < nearest.GlobalPosition.DistanceTo(first.GlobalPosition)))
            {
                nearest = other;
            }
        }

        if (nearest != null)
        {
            _gap = (first.GlobalPosition + nearest.GlobalPosition) / 2f;
            GD.Print("Bot: squeezing between " + first.Name + " and " + nearest.Name);
        }
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        if (_gap == null)
        {
            return StepResult.Failed;
        }

        _left -= delta;

        if (_left <= 0)
        {
            body.Stop();
            return StepResult.Done;
        }

        body.SteerTo(_gap.Value);
        return StepResult.Running;
    }
}
