namespace MmoGame3d.Data.Town;

using System.Data;
using Dapper;

/// <summary>
/// World state by key, and the town log. World state values are small strings the
/// caller shapes (the street lights are "working|repaired-at|by"); a table per kind of
/// thing waits until there are more kinds.
/// </summary>
public class TownStore
{
    private readonly Database _database;

    public TownStore(Database database)
    {
        _database = database;
    }

    public string? Get(string key)
    {
        using IDbConnection connection = _database.Open();
        return connection.QuerySingleOrDefault<string?>("select value from world_state where key = @key;", new { key });
    }

    public void Set(string key, string value)
    {
        using IDbConnection connection = _database.Open();
        connection.Execute(
            @"insert into world_state (key, value) values (@key, @value)
              on conflict (key) do update set value = excluded.value, updated_at = now();",
            new { key, value });
    }

    public void AddLog(string zone, string entry)
    {
        using IDbConnection connection = _database.Open();
        connection.Execute("insert into town_log (zone, entry) values (@zone, @entry);", new { zone, entry });
    }

    // The newest first, each with when it happened.
    public List<TownLogEntry> RecentLog(string zone, int count)
    {
        using IDbConnection connection = _database.Open();
        return connection.Query<TownLogEntry>(
            "select entry as Entry, at as At from town_log where zone = @zone order by at desc, id desc limit @count;",
            new { zone, count }).AsList();
    }
}

public class TownLogEntry
{
    public string Entry { get; set; } = "";
    public DateTime At { get; set; }
}
