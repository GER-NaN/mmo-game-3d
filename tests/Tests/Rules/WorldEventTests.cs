namespace MmoGame3d.Tests.Rules;

using System;
using MmoGame3d.Rules.Events;

public class WorldEventTests
{
    private static readonly WorldEventDefinition Swarm = new WorldEventDefinition(
        "drone-swarm-meadows", WorldEventDefinition.AiSwarm, WorldEventDefinition.DroneSwarm, "Drone Swarm in Meadows!", "meadows", "DroneSwarm", 10, 180, 300);

    [Fact]
    public void TheFirstStartsOneIntervalAfterTheServerAndTheNextOneAfterTheLastEnded()
    {
        WorldEventSchedule schedule = new WorldEventSchedule(300, 0);

        Assert.False(schedule.Due(299));
        Assert.True(schedule.Due(300));

        schedule.Started();
        Assert.False(schedule.Due(1000));

        schedule.Ended(420);
        Assert.False(schedule.Due(719));
        Assert.True(schedule.Due(720));
    }

    [Fact]
    public void ItIsCompletedWhenEveryDroneIsDown()
    {
        WorldEventRun run = new WorldEventRun(Swarm, 100);

        Assert.Null(run.Check(150, 3));
        Assert.Equal(WorldEventOutcome.Completed, run.Check(150, 0));
    }

    [Fact]
    public void ItTimesOutAtItsLimit()
    {
        WorldEventRun run = new WorldEventRun(Swarm, 100);

        Assert.Null(run.Check(279, 4));
        Assert.Equal(WorldEventOutcome.TimedOut, run.Check(280, 4));
    }

    [Fact]
    public void APlayerTakesPartOnceHoweverOftenTheyComeAndGo()
    {
        WorldEventRun run = new WorldEventRun(Swarm, 0);
        Guid ana = Guid.NewGuid();
        Guid bo = Guid.NewGuid();

        Assert.True(run.TakePart(ana));
        Assert.False(run.TakePart(ana));
        Assert.True(run.TakePart(bo));
        Assert.False(run.TakePart(ana));

        Assert.Equal(2, run.Players.Count);
    }

    [Fact]
    public void OnlyACompletedEventDropsOneRewardPerPlayerWhoTookPart()
    {
        WorldEventRun run = new WorldEventRun(Swarm, 0);
        run.TakePart(Guid.NewGuid());
        run.TakePart(Guid.NewGuid());
        run.TakePart(Guid.NewGuid());

        Assert.Equal(3, run.Drops(WorldEventOutcome.Completed));
        Assert.Equal(0, run.Drops(WorldEventOutcome.TimedOut));
        Assert.Equal(0, run.Drops(WorldEventOutcome.EndedByRestart));
    }
}
