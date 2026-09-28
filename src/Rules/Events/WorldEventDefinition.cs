namespace MmoGame3d.Rules.Events;
/// <summary>
/// One kind of world event, as its definition row holds it: a Drone Swarm in the meadows,
/// its family (ai-swarm), where, how many, how long, how often. The values are the
/// server's to read; an admin panel later edits the same rows.
/// </summary>
public sealed class WorldEventDefinition
{
    public WorldEventDefinition(string id, string family, string kind, string line, string zone, string spot, int count, int timeLimitSeconds, int everySeconds)
    {
        Id = id;
        Family = family;
        Kind = kind;
        Line = line;
        Zone = zone;
        Spot = spot;
        Count = count;
        TimeLimitSeconds = timeLimitSeconds;
        EverySeconds = everySeconds;
    }

    public const string AiSwarm = "ai-swarm";
    public const string DroneSwarm = "drone-swarm";

    public string Id { get; }

    // The type (ai-swarm) and the kind within it (drone-swarm).
    public string Family { get; }

    public string Kind { get; }

    // What Notifications shows: "Drone Swarm in Meadows!".
    public string Line { get; }

    public string Zone { get; }

    // A marker under the zone's Events node: where the event happens. The scene holds
    // the place, so moving it is a scene edit.
    public string Spot { get; }

    // How many enemies.
    public int Count { get; }

    public int TimeLimitSeconds { get; }

    public int EverySeconds { get; }
}
