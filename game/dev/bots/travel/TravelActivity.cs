namespace MmoGame3d.Dev;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Rules.World;

/// <summary>
/// Getting to a zone: the route from where the bot is, walked door by door (DoorStep). A
/// party pull, a wrong door or a relog lands it somewhere else, and it plans again from
/// there; a taxi ride ends where it ends.
/// </summary>
public class TravelActivity : BotActivity
{
    private const int MaxPlans = 6;

    private readonly string _to;
    private readonly List<BotFact> _gives;
    private DoorStep? _door;
    private string _doorZone = "";
    private double _inDoor;
    private int _plans;

    public TravelActivity(string zoneId)
        : this(zoneId, "travel to " + zoneId, 0)
    {
    }

    // Travel a bot may choose by itself, under a name of its own ("go back to town").
    protected TravelActivity(string zoneId, string name, int weight)
        : base(name, weight)
    {
        _to = zoneId;
        _gives = new List<BotFact> { BotFact.InZone(zoneId) };
    }

    public override IReadOnlyList<BotFact> Gives
    {
        get { return _gives; }
    }

    public override double UsualSeconds
    {
        get { return 60; }
    }

    public override BotStep? Step
    {
        get { return _door; }
    }

    public override void Begin(BotBody body)
    {
        _door = null;
        _plans = 0;
        Why = "";
        FailedWalking = false;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        if (body.ZoneId == _to)
        {
            body.Stop();
            return StepResult.Done;
        }

        if (ZoneIds.IsInstance(body.ZoneId) || body.ZoneId.Length == 0)
        {
            return StepResult.Running;
        }

        if (_door == null || _doorZone != body.ZoneId)
        {
            _plans++;

            if (_plans > MaxPlans)
            {
                Why = "planned " + MaxPlans + " times and still in " + body.ZoneId;
                return StepResult.Failed;
            }

            List<string>? route = BotRouter.Route(body.ZoneId, _to);

            if (route == null || route.Count == 0)
            {
                Why = "no way from " + body.ZoneId + " to " + _to;
                return StepResult.Failed;
            }

            GD.Print("Bot: route to " + _to + ": " + string.Join(", ", route));
            _door = new DoorStep(route[0]);
            _doorZone = body.ZoneId;
            _inDoor = 0;
            _door.Begin(body);
        }

        _inDoor += delta;
        StepResult result = _inDoor > _door.Limit ? StepResult.Failed : _door.Tick(body, delta);

        if (result == StepResult.Failed && _door.Target(body) != null && _inDoor <= _door.Limit)
        {
            FailedWalking = true;
            body.WalkFailed?.Invoke(_door);
        }

        // Done or failed, the next tick plans from wherever the bot is now.
        if (result != StepResult.Running)
        {
            _door = null;
        }

        return StepResult.Running;
    }

    public override BotActivityJudge? NewJudge()
    {
        return new TravelJudge(_to);
    }
}
