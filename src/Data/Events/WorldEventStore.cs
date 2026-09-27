namespace MmoGame3d.Data.Events;

using System.Data;
using Dapper;
using MmoGame3d.Rules.Events;

/// <summary>
/// World event definitions, runs, who took part, and each player's points. The live
/// event (its drones, its clock) is the server's, in memory; a run still marked running
/// at a start was cut by a restart.
/// </summary>
public class WorldEventStore
{
    private const string Running = "running";
    private const string Ended = "ended";

    private readonly Database _database;

    public WorldEventStore(Database database)
    {
        _database = database;
    }

    public List<WorldEventDefinition> Definitions()
    {
        using IDbConnection connection = _database.Open();
        return connection.Query<DefinitionRow>(
            "select id as Id, family as Family, kind as Kind, line as Line, zone as Zone, spot as Spot, count as Count, " +
            "time_limit_seconds as TimeLimitSeconds, every_seconds as EverySeconds from world_event_definitions where enabled order by id;")
            .Select(row => row.ToDefinition()).ToList();
    }

    // Ends every run still marked running as ended by a restart; returns how many.
    public int EndLeftRunning()
    {
        using IDbConnection connection = _database.Open();
        return connection.Execute(
            "update world_event_runs set status = @ended, ended_at = now(), outcome = @outcome where status = @running;",
            new { ended = Ended, running = Running, outcome = OutcomeText(WorldEventOutcome.EndedByRestart) });
    }

    public long Start(string definitionId)
    {
        using IDbConnection connection = _database.Open();
        return connection.ExecuteScalar<long>(
            "insert into world_event_runs (definition_id) values (@definitionId) returning id;", new { definitionId });
    }

    // Once per player per run; the point comes with it. True the first time.
    public bool TakePart(long runId, Guid playerId)
    {
        using IDbConnection connection = _database.Open();
        using IDbTransaction transaction = connection.BeginTransaction();
        int added = connection.Execute(
            "insert into world_event_participants (run_id, player_id) values (@runId, @playerId) on conflict do nothing;",
            new { runId, playerId }, transaction);

        if (added > 0)
        {
            connection.Execute(
                "insert into world_event_points (player_id, points) values (@playerId, @points) " +
                "on conflict (player_id) do update set points = world_event_points.points + excluded.points;",
                new { playerId, points = WorldEventRun.PointsForTakingPart }, transaction);
        }

        transaction.Commit();
        return added > 0;
    }

    public void End(long runId, WorldEventOutcome outcome)
    {
        using IDbConnection connection = _database.Open();
        connection.Execute(
            "update world_event_runs set status = @ended, ended_at = now(), outcome = @outcome where id = @runId;",
            new { runId, ended = Ended, outcome = OutcomeText(outcome) });
    }

    public int Points(Guid playerId)
    {
        using IDbConnection connection = _database.Open();
        return connection.ExecuteScalar<int?>("select points from world_event_points where player_id = @playerId;", new { playerId }) ?? 0;
    }

    // The latest runs, running or ended, newest first, each saying whether the player
    // took part.
    public List<WorldEventRecord> Recent(Guid playerId, int count)
    {
        using IDbConnection connection = _database.Open();
        return connection.Query<WorldEventRecord>(
            "select r.id as Id, d.line as Line, r.status = @running as Running, r.started_at as StartedAt, r.ended_at as EndedAt, " +
            "coalesce(r.outcome, '') as Outcome, " +
            "exists (select 1 from world_event_participants p where p.run_id = r.id and p.player_id = @playerId) as TookPart " +
            "from world_event_runs r join world_event_definitions d on d.id = r.definition_id " +
            "order by r.id desc limit @count;",
            new { playerId, count, running = Running }).ToList();
    }

    public static string OutcomeText(WorldEventOutcome outcome)
    {
        switch (outcome)
        {
            case WorldEventOutcome.Completed:
                return "completed";
            case WorldEventOutcome.TimedOut:
                return "timed-out";
            default:
                return "ended-by-restart";
        }
    }

    private class DefinitionRow
    {
        public string Id { get; set; } = "";
        public string Family { get; set; } = "";
        public string Kind { get; set; } = "";
        public string Line { get; set; } = "";
        public string Zone { get; set; } = "";
        public string Spot { get; set; } = "";
        public int Count { get; set; }
        public int TimeLimitSeconds { get; set; }
        public int EverySeconds { get; set; }

        public WorldEventDefinition ToDefinition()
        {
            return new WorldEventDefinition(Id, Family, Kind, Line, Zone, Spot, Count, TimeLimitSeconds, EverySeconds);
        }
    }
}

public class WorldEventRecord
{
    public long Id { get; set; }
    public string Line { get; set; } = "";
    public bool Running { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    // "completed", "timed-out", "ended-by-restart"; empty while running.
    public string Outcome { get; set; } = "";
    public bool TookPart { get; set; }
}
