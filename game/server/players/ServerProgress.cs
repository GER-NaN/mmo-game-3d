namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using MmoGame3d.Networking;
using MmoGame3d.Rules.Skills;

/// <summary>
/// Skills and careers on the server. Experience comes from what the server saw the
/// player do (walking, jumping, repairs, workbench work), never from a client's claim.
/// The owner hears of a new level at once; their progress is sent at most once a second.
/// Saved with the player.
/// </summary>
public class ServerProgress
{
    private const double SendIntervalSeconds = 1;

    private readonly ProgressNetwork _network;
    private readonly Network _session;
    private readonly Func<IEnumerable<Session>> _sessions;
    private readonly HashSet<Session> _unsent = new HashSet<Session>();
    private double _sinceSend;

    public ServerProgress(ProgressNetwork network, Network session, Func<IEnumerable<Session>> sessions)
    {
        _network = network;
        _session = session;
        _sessions = sessions;
    }

    // Raised when a career or rank changes, so what others see can follow.
    public event Action<Session>? CareerChanged;

    public void Award(Session session, SkillId skill, long xp)
    {
        if (xp <= 0)
        {
            return;
        }

        int newLevel = session.Progress.Skills.Add(skill, xp);
        session.Progress.Career.AddSkillXp(skill, xp);

        if (newLevel > 0)
        {
            _session.SendNotice(session.PeerId, SkillCatalog.Name(skill) + " is now level " + newLevel + ".");
        }

        NoteRankReady(session);
        _unsent.Add(session);
    }

    // Experience from the career's own abilities (the engineer's repair pack).
    public void AwardCareer(Session session, long xp)
    {
        session.Progress.Career.AddCareerXp(xp);
        NoteRankReady(session);
        _unsent.Add(session);
    }

    public void MissionDone(Session session)
    {
        session.Progress.Missions++;
        _unsent.Add(session);
    }

    public void Changed(Session session)
    {
        _unsent.Add(session);
        CareerChanged?.Invoke(session);
    }

    public void Tick(double delta)
    {
        foreach (Session session in _sessions())
        {
            if (session.State != SessionState.InWorld || session.Body == null)
            {
                continue;
            }

            session.Progress.SecondsPlayed += delta;
            Award(session, SkillId.Agility, session.Agility.Walked(session.Body.TakeWalkedMetres()));

            for (int jumps = session.Body.TakeJumps(); jumps > 0; jumps--)
            {
                Award(session, SkillId.Agility, session.Agility.Jumped());
            }
        }

        _sinceSend += delta;

        if (_sinceSend < SendIntervalSeconds || _unsent.Count == 0)
        {
            return;
        }

        _sinceSend = 0;

        foreach (Session session in _unsent)
        {
            if (session.State == SessionState.InWorld)
            {
                Send(session);
            }
        }

        _unsent.Clear();
    }

    public void Forget(Session session)
    {
        _unsent.Remove(session);
    }

    public void Send(Session session)
    {
        PlayerProgress progress = session.Progress;
        int[] skills = new int[SkillCatalog.All.Length];
        long[] xp = new long[SkillCatalog.All.Length];

        for (int i = 0; i < SkillCatalog.All.Length; i++)
        {
            skills[i] = (int)SkillCatalog.All[i];
            xp[i] = progress.Skills.Xp(SkillCatalog.All[i]);
        }

        PlayerCareer career = progress.Career;
        _network.SendProgress(
            session.PeerId,
            skills,
            xp,
            career.Career.HasValue ? (int)career.Career.Value : -1,
            career.Xp,
            (int)career.Rank,
            career.ClassTaken,
            progress.Level());
    }

    // Once, when the experience for the next rank is first there.
    private void NoteRankReady(Session session)
    {
        PlayerCareer career = session.Progress.Career;

        if (career.CanRankUp() && !session.RankReadyNoted)
        {
            session.RankReadyNoted = true;
            _session.SendNotice(session.PeerId, "You are ready for " + CareerCatalog.RankName(career.Rank + 1) + ". See your old professor at your college.");
        }
    }
}
