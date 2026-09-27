namespace MmoGame3d.Dev;

using System.Collections.Generic;
using MmoGame3d.BotJudging;
using MmoGame3d.Players;

/// <summary>
/// Judges how a bot moves between zones. It keeps each zone change with its time and
/// reports two things, each once per RepeatAfter (the checks themselves are ZoneChanges,
/// plain C# the unit tests drive):
///
/// - Too many: more than ZoneChanges.MaxChanges in its window, far more than a person
///   walking between places makes.
/// - Ping-pong: back and forth between the same two zones, each stay short: bounced
///   straight back, as when an arrival puts a player in the door they came through.
///
/// A ride counts: town to a taxi cabin and back is two changes.
/// </summary>
public sealed class BotZoneJudge
{
    private const double RepeatAfter = 60;

    private readonly string _profile;
    private readonly ZoneChanges _changes = new ZoneChanges();
    private readonly Dictionary<string, double> _reportedAt = new Dictionary<string, double>();
    private double _clock;
    private string _zone = "";

    public BotZoneJudge(string profile)
    {
        _profile = profile;
    }

    // planned: the step running now goes through doors, so a change is its own doing.
    public void Tick(BotBody body, double delta, string doing, bool planned)
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
            _changes.Add(_clock, _zone, zone, planned);
        }

        _zone = zone;
        IReadOnlyList<ZoneChange> recent = _changes.Recent;

        if (_changes.Churning)
        {
            Report(me, "zone-churn", recent.Count + " zone changes in " + (int)(_clock - recent[0].Time) + " s", doing);
        }

        if (_changes.PingPonging)
        {
            ZoneChange last = recent[recent.Count - 1];
            Report(me, "zone-ping-pong", "back and forth between " + last.From + " and " + last.To + " " + _changes.BackAndForth() + " times in a row", doing);
        }
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

        foreach (ZoneChange change in _changes.Recent)
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
}
