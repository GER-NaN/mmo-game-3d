namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Data;
using MmoGame3d.Data.Social;
using MmoGame3d.Networking;
using MmoGame3d.Rules.Chat;
using MmoGame3d.Rules.Skills;
using MmoGame3d.Rules.Social;
using MmoGame3d.Rules.World;

/// <summary>
/// Whois, the terminal app with a page for every player. Pages are read from the
/// database (so offline players have one too) and, for a player online, overlaid with
/// what the game knows now: level, career, where they are and what they are doing.
/// Skills show only when the owner turns them on; location goes dark away from town or
/// when turned off. Props are one per visitor per page, and can be taken back.
/// </summary>
public class ServerWhois
{
    private readonly SocialNetwork _network;
    private readonly Network _session;
    private readonly PersistenceWorker _worker;
    private readonly WhoisStore _store;
    private readonly ServerTerminals _terminals;
    private readonly Func<IEnumerable<Session>> _sessions;
    private readonly ChatFilterPipeline _filters = ChatFilterPipeline.Default();

    public ServerWhois(SocialNetwork network, Network session, PersistenceWorker worker, WhoisStore store, ServerTerminals terminals, Func<IEnumerable<Session>> sessions)
    {
        _network = network;
        _session = session;
        _worker = worker;
        _store = store;
        _terminals = terminals;
        _sessions = sessions;
    }

    // Whether someone is on the street-light job, for "what they are doing".
    public Func<Guid, bool>? HasJob { get; set; }

    public void Search(Session session, string text)
    {
        text = text.Trim();

        if (text.Length < Whois.MinSearchLength)
        {
            _session.SendNotice(session.PeerId, "Type at least " + Whois.MinSearchLength + " letters of a name.");
            return;
        }

        long peer = session.PeerId;
        _worker.Enqueue(
            () => _store.Search(text),
            rows =>
            {
                string[] ids = new string[rows.Count];
                string[] names = new string[rows.Count];
                string[] titles = new string[rows.Count];
                int[] levels = new int[rows.Count];
                int[] online = new int[rows.Count];

                for (int i = 0; i < rows.Count; i++)
                {
                    Session? live = Online(rows[i].PlayerId);
                    ids[i] = rows[i].PlayerId.ToString();
                    names[i] = rows[i].DisplayName;
                    titles[i] = live != null ? Title(live) : CareerCatalog.Title(CareerOf(rows[i].Career), (CareerRank)rows[i].CareerRank);
                    levels[i] = live != null ? live.Progress.Level() : LevelOf(rows[i]);
                    online[i] = live != null ? 1 : 0;
                }

                _network.SendWhoisResults(peer, ids, names, titles, levels, online);
            },
            e => GD.PrintErr("Whois search failed: " + e.Message));
    }

    public void Open(Session session, string playerIdText)
    {
        Guid playerId;

        if (!Guid.TryParse(playerIdText, out playerId))
        {
            return;
        }

        Guid viewerId = session.Record!.PlayerId;
        long peer = session.PeerId;
        _worker.Enqueue(
            () => _store.LoadProfile(playerId, viewerId),
            profile =>
            {
                if (profile == null)
                {
                    _session.SendNotice(peer, "There is no such player.");
                    return;
                }

                Session? viewer = null;

                foreach (Session candidate in _sessions())
                {
                    if (candidate.PeerId == peer)
                    {
                        viewer = candidate;
                    }
                }

                if (viewer != null)
                {
                    _network.SendWhoisPage(peer, Page(viewer, profile));
                }
            },
            e => GD.PrintErr("Whois page failed: " + e.Message));
    }

    public void ToggleProps(Session session, string playerIdText)
    {
        Guid playerId;

        if (!Guid.TryParse(playerIdText, out playerId) || playerId == session.Record!.PlayerId)
        {
            return;
        }

        Guid giverId = session.Record.PlayerId;
        _worker.Enqueue(() => _store.ToggleProps(giverId, playerId), given => Open(session, playerIdText), e => GD.PrintErr("Props failed: " + e.Message));
    }

    // The owner's own settings. The Plan is player text: it passes the chat filters.
    public void Edit(Session session, string plan, bool showSkills, bool showLocation)
    {
        string clean = plan.Length > WhoisSettings.MaxPlanLength * 2 ? "" : _filters.Apply(plan);

        if (clean.Length > WhoisSettings.MaxPlanLength)
        {
            clean = clean.Substring(0, WhoisSettings.MaxPlanLength);
        }

        session.Page = new WhoisSettings { Plan = clean, ShowSkills = showSkills, ShowLocation = showLocation };
        Guid playerId = session.Record!.PlayerId;
        WhoisSettings copy = new WhoisSettings { Plan = clean, ShowSkills = showSkills, ShowLocation = showLocation };
        string id = playerId.ToString();
        _worker.Enqueue(
            () =>
            {
                _store.SaveSettings(playerId, copy);
                return true;
            },
            saved => Open(session, id),
            e => GD.PrintErr("Saving a page failed: " + e.Message));
    }

    private Godot.Collections.Dictionary Page(Session viewer, WhoisProfile profile)
    {
        WhoisRow row = profile.Row;
        Session? live = Online(row.PlayerId);
        bool own = row.PlayerId == viewer.Record!.PlayerId;
        WhoisSettings settings = live != null ? live.Page : profile.Settings;

        CareerId? career = live != null ? live.Progress.Career.Career : CareerOf(row.Career);
        CareerRank rank = live != null ? live.Progress.Career.Rank : (CareerRank)row.CareerRank;
        long careerXp = live != null ? live.Progress.Career.Xp : row.CareerXp;
        float progress = 0f;
        string next = "";

        if (career.HasValue && rank < CareerRank.Elite)
        {
            long from = CareerCatalog.XpForRank(rank);
            long to = CareerCatalog.XpForRank(rank + 1);
            progress = Math.Clamp((careerXp - from) / (float)(to - from), 0f, 1f);
            next = CareerCatalog.RankName(rank + 1);
        }

        string where;
        string doing = "";

        if (live != null)
        {
            where = Whois.Where(live.ZoneId!, settings.ShowLocation);
            doing = Doing(live);
        }
        else
        {
            where = Whois.LastSeen(DateTime.SpecifyKind(row.SavedAt, DateTimeKind.Utc), DateTime.UtcNow);
        }

        bool skillsShown = settings.ShowSkills || own;
        List<int> skillIds = new List<int>();
        List<long> skillXp = new List<long>();

        if (skillsShown)
        {
            foreach (SkillId skill in SkillCatalog.All)
            {
                long xp;

                if (live != null)
                {
                    xp = live.Progress.Skills.Xp(skill);
                }
                else if (!profile.Skills.TryGetValue((int)skill, out xp))
                {
                    xp = 0;
                }

                skillIds.Add((int)skill);
                skillXp.Add(xp);
            }
        }

        Godot.Collections.Dictionary page = new Godot.Collections.Dictionary
        {
            { "id", row.PlayerId.ToString() },
            { "name", row.DisplayName },
            { "level", live != null ? live.Progress.Level() : LevelOf(row) },
            { "title", CareerCatalog.Title(career, rank) },
            { "progress", progress },
            { "next", next },
            { "online", live != null },
            { "where", where },
            { "doing", doing },
            { "plan", settings.Plan },
            { "skillsShown", skillsShown },
            { "skillIds", skillIds.ToArray() },
            { "skillXp", skillXp.ToArray() },
            { "props", profile.Props },
            { "gave", profile.GavePropsToo },
            { "friends", profile.Friends },
            { "isFriend", viewer.Contacts.IsFriend(row.PlayerId) },
            { "own", own },
            { "showSkills", settings.ShowSkills },
            { "showLocation", settings.ShowLocation },
        };
        return page;
    }

    // What they are doing, filled by the game, never typed.
    private string Doing(Session live)
    {
        if (live.Body != null && live.Body.IsOnline)
        {
            return _terminals.IsOnPhone(live) ? "On their phone" : "In the terminal";
        }

        if (ZoneIds.SceneOf(live.ZoneId!) == ZoneIds.Taxi)
        {
            return "On a robo taxi ride";
        }

        if (HasJob != null && HasJob(live.Record!.PlayerId))
        {
            return "On the street light repair";
        }

        return "Out and about";
    }

    private Session? Online(Guid playerId)
    {
        foreach (Session session in _sessions())
        {
            if (session.State == SessionState.InWorld && session.Record != null && session.Record.PlayerId == playerId)
            {
                return session;
            }
        }

        return null;
    }

    private static string Title(Session live)
    {
        return CareerCatalog.Title(live.Progress.Career.Career, live.Progress.Career.Rank);
    }

    private static CareerId? CareerOf(int? career)
    {
        return career.HasValue && CareerCatalog.Find(career.Value) != null ? (CareerId)career.Value : null;
    }

    private static int LevelOf(WhoisRow row)
    {
        return PlayerLevel.For((long)(row.SecondsPlayed / 60), row.SkillXp, row.CareerXp, row.Missions);
    }
}
