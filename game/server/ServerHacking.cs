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
/// The code cracker at public terminals: the Hacking skill's act. The code lives here;
/// the client sends guesses and gets the answers. A Computer Scientist also sees which
/// places were right, the career's "sees more in the terminal".
/// </summary>
public class ServerHacking
{
    // A code cracked: the town checks whether it was the taxis' rootkit.
    public Action<Session>? CodeCracked { get; set; }

    // Tells the achievements when one is earned here; set by ServerGame.
    public Action<Session, string>? Achieved { get; set; }

    private const int Playing = 0;
    private const int Cracked = 1;
    private const int LockedOut = 2;

    private readonly TerminalNetwork _network;
    private readonly Network _session;
    private readonly ServerTerminals _terminals;
    private readonly ServerProgress _progress;
    private readonly PersistenceWorker _worker;
    private readonly ScoreStore _scores;
    private readonly Dictionary<Session, CodeCracker> _codes = new Dictionary<Session, CodeCracker>();
    private readonly Dictionary<Session, DateTime> _started = new Dictionary<Session, DateTime>();
    private readonly Random _random = new Random();

    public ServerHacking(TerminalNetwork network, Network session, ServerTerminals terminals, ServerProgress progress, PersistenceWorker worker, ScoreStore scores)
    {
        _worker = worker;
        _scores = scores;
        _network = network;
        _session = session;
        _terminals = terminals;
        _progress = progress;
    }

    public void Start(Session session)
    {
        if (!AtPublicTerminal(session))
        {
            return;
        }

        _codes[session] = new CodeCracker(_random);
        _started[session] = DateTime.UtcNow;
        Send(session, _codes[session]);
        SendBoard(session);
    }

    public void Guess(Session session, string guess)
    {
        CodeCracker? code;

        if (!AtPublicTerminal(session) || !_codes.TryGetValue(session, out code))
        {
            return;
        }

        string? refusal = code.Guess(guess);

        if (refusal != null)
        {
            _session.SendNotice(session.PeerId, refusal);
            return;
        }

        if (code.Solved)
        {
            _session.SendNotice(session.PeerId, "Code cracked.");
            _progress.Award(session, SkillId.Hacking, SkillAwards.HackingPerCode);
            Record(session, code.Guesses.Count);
            Achieved?.Invoke(session, Rules.Achievements.Achievements.CrackACode);
            CodeCracked?.Invoke(session);
        }

        Send(session, code);
    }

    public void Forget(Session session)
    {
        _codes.Remove(session);
        _started.Remove(session);
    }

    // The result goes on the board, then the player sees the board with it.
    private void Record(Session session, int guesses)
    {
        DateTime started;
        double seconds = _started.TryGetValue(session, out started) ? (DateTime.UtcNow - started).TotalSeconds : 0;
        Guid playerId = session.Record!.PlayerId;
        string name = session.Record.DisplayName;
        _worker.Enqueue(
            () =>
            {
                _scores.Add(Leaderboards.CodeCracker, playerId, name, guesses, seconds);
                return true;
            },
            saved => SendBoard(session),
            e => GD.PrintErr("Saving a code cracker score failed: " + e.Message));
    }

    private void SendBoard(Session session)
    {
        Guid playerId = session.Record!.PlayerId;
        long peer = session.PeerId;
        bool lower = Leaderboards.LowerIsBetter(Leaderboards.CodeCracker);
        _worker.Enqueue(
            () =>
            {
                List<string> lines = new List<string>();
                List<ScoreRecord> top = _scores.Top(Leaderboards.CodeCracker, lower, Leaderboards.Shown);

                for (int i = 0; i < top.Count; i++)
                {
                    lines.Add((i + 1) + ". " + top[i].PlayerName + "   " + Leaderboards.Result(Leaderboards.CodeCracker, top[i].Score, top[i].Seconds));
                }

                ScoreRecord? best = _scores.Best(Leaderboards.CodeCracker, playerId, lower);
                lines.Add(best == null ? "Your best: none yet" : "Your best: " + Leaderboards.Result(Leaderboards.CodeCracker, best.Score, best.Seconds));
                return lines.ToArray();
            },
            lines => _network.SendBoard(peer, Leaderboards.CodeCracker, lines),
            e => GD.PrintErr("Reading the code cracker board failed: " + e.Message));
    }

    private bool AtPublicTerminal(Session session)
    {
        bool at = session.Body != null && session.Body.IsOnline && !_terminals.IsOnPhone(session);

        if (!at)
        {
            _session.SendNotice(session.PeerId, "Codes are cracked at public terminals.");
        }

        return at;
    }

    private void Send(Session session, CodeCracker code)
    {
        bool scientist = session.Progress.Career.Career == CareerId.ComputerScientist;
        string[] positions = new string[code.Positions.Count];

        for (int i = 0; i < positions.Length; i++)
        {
            positions[i] = scientist ? code.Positions[i] : "";
        }

        int status = code.Solved ? Cracked : (code.Over ? LockedOut : Playing);
        _network.SendCrack(session.PeerId, new List<string>(code.Guesses).ToArray(), new List<int>(code.Exact).ToArray(), new List<int>(code.Partial).ToArray(), positions, code.GuessesLeft, status);
    }
}
