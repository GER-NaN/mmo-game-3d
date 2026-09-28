namespace MmoGame3d.Dev;

using System.Collections.Generic;
using Godot;

/// <summary>Walks to the parts of the map not discovered yet, nearest first.</summary>
public sealed class ExploreStep : BotStep
{
    private const float Near = 6f;

    private readonly List<Vector3> _unreachable = new List<Vector3>();
    private Walker _walker = new Walker();
    private Vector3? _spot;
    private int _reached;

    public ExploreStep()
        : base("explore", 90)
    {
    }

    public override bool Walks
    {
        get { return true; }
    }

    public override Vector3? Target(BotBody body)
    {
        return _spot;
    }

    public override void Begin(BotBody body)
    {
        _spot = null;
        _reached = 0;
        _unreachable.Clear();
    }

    private bool Unreachable(Vector3 spot)
    {
        foreach (Vector3 failed in _unreachable)
        {
            if (failed.DistanceTo(spot) < Rules.Maps.Discovery.CellSize)
            {
                return true;
            }
        }

        return false;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        if (_spot == null)
        {
            Vector3? best = null;

            foreach (Vector3 spot in body.Undiscovered())
            {
                if (!Unreachable(spot) && (best == null || body.DistanceTo(spot) < body.DistanceTo(best.Value)))
                {
                    best = spot;
                }
            }

            if (best == null)
            {
                return StepResult.Done;
            }

            _spot = best;
            _walker = new Walker();
        }

        StepResult walked = _walker.Walk(body, _spot.Value, Near, delta);

        if (walked == StepResult.Running)
        {
            return StepResult.Running;
        }

        // Out of reach (inside a building, past an edge): not tried again this step.
        if (walked == StepResult.Failed)
        {
            _unreachable.Add(_spot.Value);
        }

        _spot = null;
        _reached++;
        return _reached >= 4 ? StepResult.Done : StepResult.Running;
    }
}
