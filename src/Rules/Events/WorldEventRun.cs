namespace MmoGame3d.Rules.Events;

using System;
using System.Collections.Generic;

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
