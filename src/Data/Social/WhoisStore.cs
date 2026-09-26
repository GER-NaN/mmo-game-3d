namespace MmoGame3d.Data.Social;

using System.Data;
using Dapper;
using MmoGame3d.Rules.Social;

// Whois pages and props. A player with no page row has the defaults.
public class WhoisStore
{
    private readonly Database _database;

    public WhoisStore(Database database)
    {
        _database = database;
    }

    public WhoisSettings LoadSettings(Guid playerId)
    {
        using IDbConnection connection = _database.Open();
        WhoisSettings? settings = connection.QuerySingleOrDefault<WhoisSettings>(
            "select plan as Plan, show_skills as ShowSkills, show_location as ShowLocation from player_pages where player_id = @playerId;",
            new { playerId });
        return settings ?? new WhoisSettings();
    }

    public void SaveSettings(Guid playerId, WhoisSettings settings)
    {
        using IDbConnection connection = _database.Open();
        connection.Execute(
            @"insert into player_pages (player_id, plan, show_skills, show_location) values (@playerId, @Plan, @ShowSkills, @ShowLocation)
              on conflict (player_id) do update set plan = excluded.plan, show_skills = excluded.show_skills, show_location = excluded.show_location;",
            new { playerId, settings.Plan, settings.ShowSkills, settings.ShowLocation });
    }

    // Gives props, or takes them back if already given. True when they are given now.
    public bool ToggleProps(Guid giverId, Guid receiverId)
    {
        using IDbConnection connection = _database.Open();
        int removed = connection.Execute("delete from props where giver_id = @giverId and receiver_id = @receiverId;", new { giverId, receiverId });

        if (removed > 0)
        {
            return false;
        }

        connection.Execute("insert into props (giver_id, receiver_id) values (@giverId, @receiverId);", new { giverId, receiverId });
        return true;
    }

    // Name matches, case-insensitive, most recently seen first.
    public List<WhoisRow> Search(string text)
    {
        using IDbConnection connection = _database.Open();
        string pattern = "%" + text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
        return connection.Query<WhoisRow>(
            SelectRows + " where p.display_name ilike @pattern order by p.saved_at desc limit @limit;",
            new { pattern, limit = Whois.MaxResults }).ToList();
    }

    // Everything a page needs about one player, online or not; null when there is none.
    public WhoisProfile? LoadProfile(Guid playerId, Guid viewerId)
    {
        using IDbConnection connection = _database.Open();
        WhoisRow? row = connection.QuerySingleOrDefault<WhoisRow>(SelectRows + " where p.id = @playerId;", new { playerId });

        if (row == null)
        {
            return null;
        }

        WhoisProfile profile = new WhoisProfile { Row = row, Settings = LoadSettings(playerId) };
        profile.Props = connection.ExecuteScalar<int>("select count(*) from props where receiver_id = @playerId;", new { playerId });
        profile.GavePropsToo = connection.ExecuteScalar<int>("select count(*) from props where receiver_id = @playerId and giver_id = @viewerId;", new { playerId, viewerId }) > 0;
        profile.Friends = connection.ExecuteScalar<int>("select count(*) from contacts where player_id = @playerId and not ignored;", new { playerId });

        foreach (SkillRow skill in connection.Query<SkillRow>("select skill as Skill, xp as Xp from player_skills where player_id = @playerId;", new { playerId }))
        {
            profile.Skills[skill.Skill] = skill.Xp;
        }

        return profile;
    }

    private const string SelectRows =
        @"select p.id as PlayerId, p.display_name as DisplayName, p.zone as Zone, p.saved_at as SavedAt,
                 g.career as Career, coalesce(g.career_xp, 0) as CareerXp, coalesce(g.career_rank, 0) as CareerRank,
                 coalesce(g.seconds_played, 0) as SecondsPlayed, coalesce(g.missions, 0) as Missions,
                 coalesce((select sum(xp) from player_skills s where s.player_id = p.id), 0) as SkillXp
          from players p left join player_progress g on g.player_id = p.id";

    private class SkillRow
    {
        public int Skill { get; set; }
        public long Xp { get; set; }
    }
}

public class WhoisRow
{
    public Guid PlayerId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Zone { get; set; } = "";
    public DateTime SavedAt { get; set; }
    public int? Career { get; set; }
    public long CareerXp { get; set; }
    public int CareerRank { get; set; }
    public double SecondsPlayed { get; set; }
    public long Missions { get; set; }
    public long SkillXp { get; set; }
}

public class WhoisProfile
{
    public WhoisRow Row { get; set; } = new WhoisRow();
    public WhoisSettings Settings { get; set; } = new WhoisSettings();
    public Dictionary<int, long> Skills { get; } = new Dictionary<int, long>();
    public int Props { get; set; }
    public bool GavePropsToo { get; set; }
    public int Friends { get; set; }
}
