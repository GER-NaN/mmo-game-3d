namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Data;
using MmoGame3d.Data.Events;
using MmoGame3d.Networking;
using MmoGame3d.Rules.Events;
using MmoGame3d.Rules.Items;
using MmoGame3d.Zones;

/// <summary>
/// World events (world.md section 2): each definition on its schedule, whether or not
/// anyone is online. A Drone Swarm sends its drones over its spot; a player in its area
/// while it runs takes part (once, a world event point with it); it is completed when
/// every drone is down, which scatters a GPU core per player who took part round the
/// spot, or times out, and the drones leave. The live event is only in memory: a run
/// the database still has running at a start was cut by a restart.
/// </summary>
public class ServerWorldEvents
{
    public const string SpotsNode = "Events";

    private const int RowsShown = 10;
    private const float DropSpread = 6f;

    private readonly WorldEventStore _store;
    private readonly PersistenceWorker _worker;
    private readonly TerminalNetwork _network;
    private readonly World _world;
    private readonly Func<IEnumerable<Session>> _sessions;
    private readonly Func<string, ServerDrones?> _drones;
    private readonly GroundItems _ground;
    private readonly List<Scheduled> _scheduled = new List<Scheduled>();
    private readonly Random _random = new Random();
    private double _clock;

    public ServerWorldEvents(WorldEventStore store, PersistenceWorker worker, TerminalNetwork network, World world, Func<IEnumerable<Session>> sessions, Func<string, ServerDrones?> drones, GroundItems ground)
    {
        _store = store;
        _worker = worker;
        _network = network;
        _world = world;
        _sessions = sessions;
        _drones = drones;
        _ground = ground;
    }

    // What happens here, for the terminal's status board; set by ServerGame.
    public Action<string>? Post { get; set; }

    // A time as the world's clock reads it (hh:mm); set by ServerGame.
    public Func<DateTime, string> TimeOf { get; set; } = at => at.ToString("HH:mm");

    public void Load()
    {
        _worker.Enqueue(
            () =>
            {
                int cut = _store.EndLeftRunning();

                if (cut > 0)
                {
                    GD.Print("World events: " + cut + " left running by the last server, ended by the restart");
                }

                return _store.Definitions();
            },
            definitions =>
            {
                foreach (WorldEventDefinition definition in definitions)
                {
                    if (_drones(definition.Zone) == null || Spot(definition) == null)
                    {
                        GD.PrintErr("World event " + definition.Id + ": " + definition.Zone + " has no drones or no spot " + definition.Spot);
                        continue;
                    }

                    _scheduled.Add(new Scheduled(definition, new WorldEventSchedule(definition.EverySeconds, _clock)));
                    GD.Print("World event " + definition.Id + ": every " + definition.EverySeconds + " s, the first in " + definition.EverySeconds + " s");
                }
            },
            e => GD.PrintErr("Loading world events failed: " + e.Message));
    }

    public void Tick(double delta)
    {
        _clock += delta;

        foreach (Scheduled scheduled in _scheduled)
        {
            if (scheduled.Run == null)
            {
                if (scheduled.Schedule.Due(_clock))
                {
                    Start(scheduled);
                }

                continue;
            }

            foreach (Session session in _sessions())
            {
                if (InArea(session, scheduled.Run.Definition) && scheduled.Run.TakePart(session.Record!.PlayerId))
                {
                    TakePart(scheduled, session);
                }
            }

            WorldEventOutcome? outcome = scheduled.Run.Check(_clock, _drones(scheduled.Definition.Zone)!.Flying);

            if (outcome.HasValue)
            {
                End(scheduled, outcome.Value);
            }
        }
    }

    // The Notifications app: the running events, the ones ended, the player's points.
    public void Send(Session session)
    {
        if (session.Record == null)
        {
            return;
        }

        Guid playerId = session.Record.PlayerId;
        long peer = session.PeerId;
        _worker.Enqueue(
            () => new Rows(_store.Points(playerId), _store.Recent(playerId, RowsShown)),
            rows =>
            {
                List<string> current = new List<string>();
                List<string> past = new List<string>();

                foreach (WorldEventRecord record in rows.Records)
                {
                    if (record.Running)
                    {
                        current.Add(record.Line + "   since " + TimeOf(record.StartedAt));
                    }
                    else
                    {
                        string ended = record.EndedAt.HasValue ? TimeOf(record.EndedAt.Value) : "?";
                        past.Add(record.Line + "   " + TimeOf(record.StartedAt) + " to " + ended + "   " + OutcomeLine(record.Outcome) + (record.TookPart ? "   you took part" : ""));
                    }
                }

                _network.SendEvents(peer, rows.Points, current.ToArray(), past.ToArray());
            },
            e => GD.PrintErr("Reading world events failed: " + e.Message));
    }

    // The area a player takes part in: the event's zone, for now.
    private static bool InArea(Session session, WorldEventDefinition definition)
    {
        return session.State == SessionState.InWorld && session.Record != null && session.ZoneId == definition.Zone;
    }

    private Vector3? Spot(WorldEventDefinition definition)
    {
        Zone? zone = _world.GetZone(definition.Zone);
        Node3D? marker = zone?.GetNodeOrNull<Node3D>(SpotsNode + "/" + definition.Spot);
        return marker?.Position;
    }

    private void Start(Scheduled scheduled)
    {
        WorldEventDefinition definition = scheduled.Definition;
        int count = definition.Count;
        scheduled.Schedule.Started();
        scheduled.Run = new WorldEventRun(definition, _clock);
        RunId id = new RunId();
        scheduled.Id = id;

        _drones(definition.Zone)!.SpawnSwarm(Spot(definition)!.Value, count);
        GD.Print("World event " + definition.Id + " started: " + count + " drones");
        Post?.Invoke(definition.Line);

        // The id is set and read on the worker's thread, in queue order, so a player
        // taking part before the insert has come back still lands on this run.
        _worker.Enqueue(
            () =>
            {
                id.Value = _store.Start(definition.Id);
                return id.Value;
            },
            _ => SendToOnline(),
            e => GD.PrintErr("Starting world event " + definition.Id + " failed: " + e.Message));
    }

    private void TakePart(Scheduled scheduled, Session session)
    {
        RunId id = scheduled.Id!;
        Guid playerId = session.Record!.PlayerId;
        GD.Print("World event " + scheduled.Definition.Id + ": " + session.Record.DisplayName + " takes part");
        _worker.Enqueue(() => _store.TakePart(id.Value, playerId), e => GD.PrintErr("Saving a world event part failed: " + e.Message));
    }

    private void End(Scheduled scheduled, WorldEventOutcome outcome)
    {
        WorldEventRun run = scheduled.Run!;
        WorldEventDefinition definition = run.Definition;
        RunId id = scheduled.Id!;
        ServerDrones drones = _drones(definition.Zone)!;
        int drops = run.Drops(outcome);
        Zone zone = _world.GetZone(definition.Zone)!;
        Vector3 spot = Spot(definition)!.Value;

        for (int i = 0; i < drops; i++)
        {
            float angle = (float)(_random.NextDouble() * Mathf.Tau);
            float distance = 1f + (float)(_random.NextDouble() * DropSpread);
            Vector3 at = spot + new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
            float? ground = SpaceQueries.GroundUnder(zone, at, 50f, 100f);
            at.Y = (ground ?? spot.Y) + 0.1f;
            _ground.DropAt(zone, at, ItemType.GpuCore, ItemTier.Standard, 1);
        }

        // A swarm that timed out flies off; a completed one lies where it fell and goes
        // as downed drones do.
        if (outcome != WorldEventOutcome.Completed)
        {
            drones.Clear();
        }

        scheduled.Run = null;
        scheduled.Id = null;
        scheduled.Schedule.Ended(_clock);
        GD.Print("World event " + definition.Id + " ended: " + WorldEventStore.OutcomeText(outcome) + ", " + run.Players.Count + " took part, " + drops + " GPU cores dropped");
        Post?.Invoke(definition.Line + " " + OutcomeLine(WorldEventStore.OutcomeText(outcome)) + ".");
        _worker.Enqueue(() => _store.End(id.Value, outcome), e => GD.PrintErr("Ending world event " + definition.Id + " failed: " + e.Message));

        // After the end is written: the worker keeps queue order.
        _worker.Enqueue(() => true, _ => SendToOnline(), e => { });
    }

    // Everyone with a screen open (a terminal or the phone) sees the change at once.
    private void SendToOnline()
    {
        foreach (Session session in _sessions())
        {
            if (session.State == SessionState.InWorld && session.Body != null && session.Body.IsOnline)
            {
                Send(session);
            }
        }
    }

    private static string OutcomeLine(string outcome)
    {
        switch (outcome)
        {
            case "completed":
                return "completed";
            case "timed-out":
                return "timed out";
            case "ended-by-restart":
                return "ended by a restart";
            default:
                return outcome;
        }
    }

    // Written on the worker's thread only.
    private sealed class RunId
    {
        public long Value;
    }

    private sealed class Rows
    {
        public Rows(int points, List<WorldEventRecord> records)
        {
            Points = points;
            Records = records;
        }

        public int Points { get; }

        public List<WorldEventRecord> Records { get; }
    }

    private sealed class Scheduled
    {
        public Scheduled(WorldEventDefinition definition, WorldEventSchedule schedule)
        {
            Definition = definition;
            Schedule = schedule;
        }

        public WorldEventDefinition Definition { get; }

        public WorldEventSchedule Schedule { get; }

        public WorldEventRun? Run { get; set; }

        public RunId? Id { get; set; }
    }
}
