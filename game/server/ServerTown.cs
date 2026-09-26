namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using MmoGame3d.Data;
using MmoGame3d.Data.Town;
using MmoGame3d.Networking;
using MmoGame3d.Rules.Social;
using MmoGame3d.Rules.Time;
using MmoGame3d.Rules.Town;
using MmoGame3d.Town;

/// <summary>
/// The town's shared state and its jobs: the street lights and the robo taxis' rootkit.
/// It keeps their state (saved, so it survives a restart), who has taken each job, and
/// the town log; it shows them to everyone through the synced TownState, and it feeds
/// the terminal's Town repairs and Town log apps.
/// </summary>
public class ServerTown
{
    public const string ZoneId = "town";

    private const string LightsKey = "town.streetlights";
    private const string TaxisKey = "town.taxirootkit";
    private const double CheckSeconds = 30;
    private const int LogKept = 20;

    private readonly TownStore _store;
    private readonly PersistenceWorker _worker;
    private readonly Network _session;
    private readonly TerminalNetwork _terminal;
    private readonly ServerChat _chat;
    private readonly WorldClock _clock;
    private readonly Func<IEnumerable<Session>> _sessions;
    private readonly Action<Session> _bagChanged;
    private readonly TownState _state;
    private readonly HashSet<Guid> _jobTakers = new HashSet<Guid>();
    private readonly HashSet<Guid> _taxiJobTakers = new HashSet<Guid>();
    private TaxiRootkit _taxis = TaxiRootkit.Infected();
    private readonly List<string> _log = new List<string>();
    private StreetLights _lights = StreetLights.Broken();
    private double _sinceCheck;

    public ServerTown(TownState state, TownStore store, PersistenceWorker worker, Network session, TerminalNetwork terminal,
        ServerChat chat, WorldClock clock, Func<IEnumerable<Session>> sessions, Action<Session> bagChanged)
    {
        _state = state;
        _store = store;
        _worker = worker;
        _session = session;
        _terminal = terminal;
        _chat = chat;
        _clock = clock;
        _sessions = sessions;
        _bagChanged = bagChanged;
    }

    // At start, before any player: read straight from the database, since nothing can
    // wait on it yet.
    public void Load()
    {
        _lights = Parse(_store.Get(LightsKey));
        _state.LightsWorking = _lights.Working;
        _taxis = ParseTaxis(_store.Get(TaxisKey));
        _state.TaxisClean = _taxis.Clean;

        foreach (TownLogEntry entry in _store.RecentLog(ZoneId, LogKept))
        {
            _log.Add(entry.Entry);
        }

        GD.Print("Town: the street lights are " + (_lights.Working ? "working" : "out") + "; the robo taxis are " + (_taxis.Clean ? "clean" : "rootkitted"));
    }

    public bool TaxisClean
    {
        get { return _taxis.Clean; }
    }

    public void TakeJob(Session session, string jobId)
    {
        if (jobId == TaxiRootkit.JobId)
        {
            if (_taxis.Clean)
            {
                _session.SendNotice(session.PeerId, "The robo taxis are clean right now.");
                return;
            }

            _taxiJobTakers.Add(session.Record!.PlayerId);
            _session.SendNotice(session.PeerId, "Job taken: " + TaxiRootkit.JobTitle + ". " + TaxiRootkit.JobText);
            SendTown(session);
            return;
        }

        if (_lights.Working)
        {
            _session.SendNotice(session.PeerId, "Nothing needs repairing right now.");
            return;
        }

        _jobTakers.Add(session.Record!.PlayerId);
        _session.SendNotice(session.PeerId, "Job taken: " + StreetLights.JobTitle + ". " + StreetLights.JobText);
        SendTown(session);
    }

    // Raised when the AI takes the lights out, for the phones.
    public event Action? LightsBroke;

    // Raised when the AI gets a rootkit into the taxis, for the phones; and for the one
    // who cleaned it out.
    public event Action? TaxisInfected;
    public event Action<Session>? TaxisCleaned;

    // Raised for the one who repaired the lights: experience and a mission.
    public event Action<Session>? Repaired;

    public bool HasJob(Guid playerId)
    {
        return _jobTakers.Contains(playerId) || _taxiJobTakers.Contains(playerId);
    }

    // Dev test scenarios only: the lights out and this player on the job.
    public void DevLightsJob(Guid playerId)
    {
        if (_lights.Working)
        {
            _lights = StreetLights.Broken();
            _state.LightsWorking = false;
        }

        _jobTakers.Add(playerId);
    }

    // Dev test scenarios only: the taxis clean, so one can be called.
    public void DevCleanTaxis()
    {
        _taxis.CleanOut("a test", DateTime.UtcNow);
        _state.TaxisClean = true;
    }

    // Dev test scenarios only: the taxis infected and this player on the job.
    public void DevRootkitJob(Guid playerId)
    {
        if (_taxis.Clean)
        {
            _taxis = TaxiRootkit.Infected();
            _state.TaxisClean = false;
        }

        _taxiJobTakers.Add(playerId);
    }

    // A code cracked at a public terminal: with the rootkit job, it is the rootkit's.
    public void CodeCracked(Session session)
    {
        if (_taxis.CannotClean(_taxiJobTakers.Contains(session.Record!.PlayerId)) != null)
        {
            return;
        }

        string name = session.Record.DisplayName;
        _taxis.CleanOut(name, DateTime.UtcNow);
        _taxiJobTakers.Clear();
        TaxisCleaned?.Invoke(session);
        Changed(name + " cleaned the rootkit out of the robo taxis.");
    }

    public void Repair(Session session)
    {
        string? refusal = _lights.CannotRepair(_jobTakers.Contains(session.Record!.PlayerId), session.Inventory!);

        if (refusal != null)
        {
            _session.SendNotice(session.PeerId, refusal);
            return;
        }

        string name = session.Record.DisplayName;
        _lights.Repair(session.Inventory!, name, DateTime.UtcNow);
        session.Body?.Show(Gestures.Repair);
        _jobTakers.Clear();
        _bagChanged(session);
        Repaired?.Invoke(session);
        Changed(name + " repaired the street lights on Main Street.");
    }

    public void Tick(double delta)
    {
        _sinceCheck += delta;

        if (_sinceCheck < CheckSeconds)
        {
            return;
        }

        _sinceCheck = 0;

        if (_lights.BreakIfDue(DateTime.UtcNow))
        {
            Changed("The street lights on Main Street went dark again. The AI is at the grid.");
            LightsBroke?.Invoke();
        }

        if (_taxis.InfectIfDue(DateTime.UtcNow))
        {
            Changed("The AI slipped a rootkit into the robo taxis. No rides until it is cleaned out.");
            TaxisInfected?.Invoke();
        }
    }

    public void SendTown(Session session)
    {
        bool taken = session.Record != null && _jobTakers.Contains(session.Record.PlayerId);
        bool taxiTaken = session.Record != null && _taxiJobTakers.Contains(session.Record.PlayerId);
        _terminal.SendTown(session.PeerId, _lights.Working, taken, _taxis.Clean, taxiTaken, _log.ToArray());
    }

    private void Changed(string what)
    {
        _state.LightsWorking = _lights.Working;
        _state.TaxisClean = _taxis.Clean;

        TimeSpan now = TimeSpan.FromSeconds(_clock.SecondsOfDay(DateTime.UtcNow));
        string entry = now.ToString(@"hh\:mm", CultureInfo.InvariantCulture) + "  " + what;
        _log.Insert(0, entry);

        if (_log.Count > LogKept)
        {
            _log.RemoveAt(_log.Count - 1);
        }

        string saved = Format(_lights);
        string savedTaxis = Format(_taxis.Clean, _taxis.CleanedAtUtc, _taxis.CleanedBy);
        _worker.Enqueue(() =>
        {
            _store.Set(LightsKey, saved);
            _store.Set(TaxisKey, savedTaxis);
            _store.AddLog(ZoneId, entry);
        }, e => GD.PrintErr("Saving the town failed: " + e.Message));

        _chat.Announce(what);

        foreach (Session session in _sessions())
        {
            if (session.State == SessionState.InWorld)
            {
                SendTown(session);
            }
        }
    }

    // "1|2026-09-26T12:00:00Z|Alice": working, when repaired, by whom.
    private static string Format(StreetLights lights)
    {
        return Format(lights.Working, lights.RepairedAtUtc, lights.RepairedBy);
    }

    private static string Format(bool good, DateTime? when, string by)
    {
        string at = when == null ? "" : when.Value.ToString("o", CultureInfo.InvariantCulture);
        return (good ? "1" : "0") + "|" + at + "|" + by;
    }

    private static TaxiRootkit ParseTaxis(string? saved)
    {
        if (saved == null)
        {
            return TaxiRootkit.Infected();
        }

        string[] parts = saved.Split('|', 3);
        DateTime at;
        DateTime? cleanedAt = parts.Length > 1 && DateTime.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out at) ? at : null;
        return TaxiRootkit.Restore(parts[0] == "1", cleanedAt, parts.Length > 2 ? parts[2] : "");
    }

    private static StreetLights Parse(string? saved)
    {
        if (saved == null)
        {
            return StreetLights.Broken();
        }

        string[] parts = saved.Split('|', 3);
        DateTime at;
        DateTime? repairedAt = parts.Length > 1 && DateTime.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out at) ? at : null;
        return StreetLights.Restore(parts[0] == "1", repairedAt, parts.Length > 2 ? parts[2] : "");
    }
}
