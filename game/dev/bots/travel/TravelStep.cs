namespace MmoGame3d.Dev;

using Godot;

/// <summary>
/// A travel as one step of an activity, for one whose zone depends on a choice it makes
/// (a public terminal in town, or the phone anywhere).
/// </summary>
public sealed class TravelStep : BotStep
{
    private readonly TravelActivity _travel;

    public TravelStep(string zoneId)
        : base("travel to " + zoneId, 300)
    {
        _travel = new TravelActivity(zoneId);
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
        return _travel.Step?.Target(body);
    }

    public override void Begin(BotBody body)
    {
        _travel.Begin(body);
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        return _travel.Tick(body, delta);
    }
}
