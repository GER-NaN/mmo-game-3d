namespace MmoGame3d.Bots;

using Godot;
using MmoGame3d.Players;

/// <summary>
/// Walking, and not getting anywhere: under a couple of metres in a quarter of a minute.
/// Only while a step means to walk; standing at a terminal or pushing an edge on purpose
/// is not stuck.
/// </summary>
public class StuckWatcher : BotWatcher
{
    private const double WindowSeconds = 15;
    private const float MinDistance = 2f;

    private Vector3 _from;
    private double _for;
    private bool _watching;

    public StuckWatcher()
        : base("stuck", true)
    {
    }

    public override string? Look(BotBody body, BotStep? step, double delta)
    {
        Player? player = body.Player;

        if (player == null || step == null || step.Intent != BotIntent.Walking)
        {
            _watching = false;
            return null;
        }

        if (!_watching || player.GlobalPosition.DistanceTo(_from) >= MinDistance)
        {
            _watching = true;
            _from = player.GlobalPosition;
            _for = 0;
            return null;
        }

        _for += delta;

        if (_for < WindowSeconds)
        {
            return null;
        }

        _for = 0;
        return "under " + MinDistance + " m in " + WindowSeconds + " s while \"" + step.Name + "\"";
    }
}
