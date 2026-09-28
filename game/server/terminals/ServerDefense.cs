namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Data;
using MmoGame3d.Data.Scores;
using MmoGame3d.Networking;
using MmoGame3d.Rules.Skills;
using MmoGame3d.Rules.Terminals;

/// <summary>
/// Agent Defense on the server: it picks the seed, keeps when the run started, and
/// scores the presses the client sends against the same chart. The client's own count is
/// only a preview. A run sent back sooner than it could have been played is not scored.
/// </summary>
public class ServerDefense
{
    // Placeholders: what a run pays. Points are in the thousands.
    private const int PointsPerDollar = 2000;
    private const int PointsPerXp = 400;

    private readonly TerminalNetwork _network;
    private readonly Network _session;
    private readonly ServerProgress _progress;
    private readonly PersistenceWorker _worker;
    private readonly ScoreStore _scores;
    private readonly Action<Session> _bagChanged;
    private readonly Dictionary<Session, int> _seeds = new Dictionary<Session, int>();
    private readonly Dictionary<Session, DateTime> _started = new Dictionary<Session, DateTime>();
    private readonly Dictionary<Session, int> _lengths = new Dictionary<Session, int>();
    private readonly Random _random = new Random();

    public ServerDefense(TerminalNetwork network, Network session, ServerProgress progress, PersistenceWorker worker, ScoreStore scores, Action<Session> bagChanged)
    {
        _network = network;
        _session = session;
        _progress = progress;
        _worker = worker;
        _scores = scores;
        _bagChanged = bagChanged;
    }

    // What happens here, for the terminal's status board; set by ServerGame.
    public Action<string>? Post { get; set; }

    // Tells the achievements when one is earned here; set by ServerGame.
    public Action<Session, string>? Achieved { get; set; }

    // The leaderboard, sent the same way as the code cracker's; set by ServerGame.
    public Action<Session, string>? SendBoard { get; set; }

    public void Start(Session session)
    {
        if (session.Body == null || !session.Body.IsOnline)
        {
            return;
        }

        int seed = _random.Next();
        int length = AgentDefense.LengthMs;

        _seeds[session] = seed;
        _started[session] = DateTime.UtcNow;
        _lengths[session] = length;
        _network.SendDefenseSeed(session.PeerId, seed, length);
        SendBoard?.Invoke(session, Leaderboards.AgentDefense);
    }

    public void Finish(Session session, int[] packed)
    {
        int seed;
        DateTime started;
        int length;

        if (!_seeds.TryGetValue(session, out seed) || !_started.TryGetValue(session, out started) || !_lengths.TryGetValue(session, out length))
        {
            return;
        }

        _seeds.Remove(session);
        _started.Remove(session);
        _lengths.Remove(session);
        double seconds = (DateTime.UtcNow - started).TotalSeconds;

        if (seconds < length / 1000.0)
        {
            _session.SendNotice(session.PeerId, "Agent Defense: that run ended too soon to count.");
            return;
        }

        DefenseResult result = AgentDefense.Score(AgentDefense.Chart(seed, length), AgentDefense.Unpack(packed));
        int dollars = result.Points / PointsPerDollar;
        session.Dollars += dollars;
        _bagChanged(session);
        _progress.Award(session, SkillId.Hacking, result.Points / PointsPerXp);
        _session.SendNotice(session.PeerId, "Agent Defense: " + result.Points + " points, " + result.Perfect + " perfect, " + result.Good + " good, "
            + result.Missed + " missed, best combo " + result.BestCombo + ". The grid pays you $" + dollars + ".");
        Achieved?.Invoke(session, Rules.Achievements.Achievements.AgentDefense);
        Post?.Invoke(session.Record!.DisplayName + " held the grid in Agent Defense: " + result.Points + " points.");

        Guid playerId = session.Record!.PlayerId;
        string name = session.Record.DisplayName;
        int points = result.Points;
        _worker.Enqueue(
            () =>
            {
                _scores.Add(Leaderboards.AgentDefense, playerId, name, points, seconds);
                return true;
            },
            saved => SendBoard?.Invoke(session, Leaderboards.AgentDefense),
            e => GD.PrintErr("Saving an Agent Defense score failed: " + e.Message));
    }

    public void Forget(Session session)
    {
        _seeds.Remove(session);
        _started.Remove(session);
        _lengths.Remove(session);
    }
}
