namespace MmoGame3d.Data.Town;

using System.Data;
using Dapper;
using MmoGame3d.Rules.Town;

// The subway walls' tags. A tag is written once and never changed or removed.
public class SubwayStore
{
    private const string Select = "select id as Id, player_name as Name, paint as Paint from subway_tags";

    private readonly Database _database;

    public SubwayStore(Database database)
    {
        _database = database;
    }

    // The player's tag on the wall: new if they had none (made is true), else the one
    // they sprayed before, unchanged.
    public SubwayTag Spray(string wall, Guid playerId, string playerName, uint paint, out bool made)
    {
        using IDbConnection connection = _database.Open();
        long? id = connection.ExecuteScalar<long?>(
            "insert into subway_tags (wall, player_id, player_name, paint) values (@wall, @playerId, @playerName, @paint) " +
            "on conflict (wall, player_id) do nothing returning id;",
            new { wall, playerId, playerName, paint = (long)paint });
        made = id.HasValue;
        return connection.QuerySingle<TagRow>(Select + " where wall = @wall and player_id = @playerId;", new { wall, playerId }).ToTag();
    }

    // The newest, oldest first, as the wall shows them.
    public List<SubwayTag> Newest(string wall, int count)
    {
        using IDbConnection connection = _database.Open();
        List<SubwayTag> tags = connection.Query<TagRow>(Select + " where wall = @wall order by id desc limit @count;", new { wall, count })
            .Select(row => row.ToTag()).ToList();
        tags.Reverse();
        return tags;
    }

    // The book: every tag in the order sprayed, a page at a time.
    public List<SubwayTag> Page(string wall, int offset, int count)
    {
        using IDbConnection connection = _database.Open();
        return connection.Query<TagRow>(Select + " where wall = @wall order by id offset @offset limit @count;", new { wall, offset, count })
            .Select(row => row.ToTag()).ToList();
    }

    public int Count(string wall)
    {
        using IDbConnection connection = _database.Open();
        return connection.ExecuteScalar<int>("select count(*) from subway_tags where wall = @wall;", new { wall });
    }

    // Postgres has no unsigned int: the paint is kept in a bigint.
    private class TagRow
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public long Paint { get; set; }

        public SubwayTag ToTag()
        {
            return new SubwayTag { Id = Id, Name = Name, Paint = (uint)Paint };
        }
    }
}
