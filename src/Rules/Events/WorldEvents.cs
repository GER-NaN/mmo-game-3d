namespace MmoGame3d.Rules.Events;

using System;
using System.Collections.Generic;

// How a world event ended. The swarm reaching its own goal (destroying something,
// hacking a thing) comes later, as another outcome (world.md section 2).
public enum WorldEventOutcome
{
    Completed,
    TimedOut,
    EndedByRestart,
}

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

/// <summary>
/// When a definition's next event starts: one interval after the server starts, then
/// one interval after the last ended. One at a time; it does not wait for players.
/// </summary>
public sealed class WorldEventSchedule
{
    private readonly int _everySeconds;
    private double _nextAt;
    private bool _running;

    public WorldEventSchedule(int everySeconds, double now)
    {
        _everySeconds = everySeconds;
        _nextAt = now + everySeconds;
    }

    public double NextAt
    {
        get { return _nextAt; }
    }

    public bool Due(double now)
    {
        return !_running && now >= _nextAt;
    }

    public void Started()
    {
        _running = true;
    }

    public void Ended(double now)
    {
        _running = false;
        _nextAt = now + _everySeconds;
    }

    // Brought forward, for a dev scenario that wants one at once.
    public void StartNow(double now)
    {
        _nextAt = now;
    }
}

/// <summary>
/// One running event: who took part (once each, however often they come and go), and
/// how it ends: completed when every enemy is down, timed out at its limit. A completed
/// event drops one reward per player who took part; every player who took part gets one
/// world event point, whatever the outcome.
/// </summary>
public sealed class WorldEventRun
{
    public const int PointsForTakingPart = 1;

    private readonly HashSet<Guid> _players = new HashSet<Guid>();

    public WorldEventRun(WorldEventDefinition definition, double startedAt)
    {
        Definition = definition;
        StartedAt = startedAt;
    }

    public WorldEventDefinition Definition { get; }

    public double StartedAt { get; }

    public IReadOnlyCollection<Guid> Players
    {
        get { return _players; }
    }

    // A player seen in the event's area while it runs. True the first time.
    public bool TakePart(Guid player)
    {
        return _players.Add(player);
    }

    // How it ends now, or null while it goes on.
    public WorldEventOutcome? Check(double now, int enemiesLeft)
    {
        if (enemiesLeft <= 0)
        {
            return WorldEventOutcome.Completed;
        }

        if (now - StartedAt >= Definition.TimeLimitSeconds)
        {
            return WorldEventOutcome.TimedOut;
        }

        return null;
    }

    public int Drops(WorldEventOutcome outcome)
    {
        return outcome == WorldEventOutcome.Completed ? _players.Count : 0;
    }
}
