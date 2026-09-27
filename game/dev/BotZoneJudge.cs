namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using MmoGame3d.Players;

/// <summary>
/// Judges how a bot moves between zones. It keeps each zone change with its time and
/// reports two things, each once per RepeatAfter:
///
/// - Too many: more than MaxChanges zone changes in Window, far more than a person
///   walking between places makes.
/// - Ping-pong: back and forth between the same two zones (A, B, A, B...) PingPongs
///   times in a row, as when a door or an arrival puts a player straight back where
///   they came from.
///
/// A ride counts: town to a taxi cabin and back is two changes.
/// </summary>
public sealed class BotZoneJudge
{
    private const double Window = 300;
    private const int MaxChanges = 8;
    private const int PingPongs = 3;
    private const double RepeatAfter = 600;

    private readonly string _profile;
    private readonly List<Change> _changes = new List<Change>();
    private readonly Dictionary<string, double> _reportedAt = new Dictionary<string, double>();
    private double _clock;
    private string _zone = "";

    public BotZoneJudge(string profile)
    {
        _profile = profile;
    }

    public void Tick(BotBody body, double delta, string doing)
    {
        _clock += delta;
        Player? me = body.Me;

        // Each ride has its own cabin (taxi-8, taxi-24); they are one place here.
        string zone = body.ZoneId.StartsWith("taxi") ? "taxi" : body.ZoneId;

        if (me == null || zone.Length == 0 || zone == _zone)
        {
            return;
        }

        if (_zone.Length > 0)
        {
            _changes.Add(new Change(_clock, _zone, zone));
        }

        _zone = zone;

        while (_changes.Count > 0 && _changes[0].Time < _clock - Window)
        {
            _changes.RemoveAt(0);
        }

        if (_changes.Count > MaxChanges)
        {
            Report(me, "zone-churn", _changes.Count + " zone changes in " + (int)(_clock - _changes[0].Time) + " s", doing);
        }

        int back = BackAndForth();

        if (back >= PingPongs)
        {
            Change last = _changes[_changes.Count - 1];
            Report(me, "zone-ping-pong", "back and forth between " + last.From + " and " + last.To + " " + back + " times in a row", doing);
        }
    }

    // How many of the latest changes, in a row, go between the same two zones and turn
    // straight back: A to B, B to A, A to B.
    private int BackAndForth()
    {
        int count = 0;

        for (int i = _changes.Count - 1; i > 0; i--)
        {
            Change later = _changes[i];
            Change earlier = _changes[i - 1];

            if (later.From != earlier.To || later.To != earlier.From)
            {
                break;
            }

            count++;
        }

        return count;
    }

    private void Report(Player me, string kind, string detail, string doing)
    {
        double last;

        if (_reportedAt.TryGetValue(kind, out last) && _clock - last < RepeatAfter)
        {
            return;
        }

        _reportedAt[kind] = _clock;
        List<string> recent = new List<string>();

        foreach (Change change in _changes)
        {
            recent.Add((int)(_clock - change.Time) + " s ago: " + change.From + " to " + change.To);
        }

        BotFindings.Write(me, _profile, kind, detail, new Dictionary<string, object?>
        {
            { "zone", _zone },
            { "activity", doing },
            { "changes", recent },
        });
    }

    private sealed class Change
    {
        public Change(double time, string from, string to)
        {
            Time = time;
            From = from;
            To = to;
        }

        public double Time { get; }

        public string From { get; }

        public string To { get; }
    }
}
