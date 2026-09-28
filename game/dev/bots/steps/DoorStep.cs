namespace MmoGame3d.Dev;
using Godot;

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
