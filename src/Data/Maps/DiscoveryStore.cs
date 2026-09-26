namespace MmoGame3d.Data.Maps;

using System.Data;
using Dapper;

// Where each player has been, per zone: the bit sets from Discovery.
public class DiscoveryStore
{
    private readonly Database _database;

    public DiscoveryStore(Database database)
    {
        _database = database;
    }

    public Dictionary<string, byte[]> Load(Guid playerId)
    {
        using IDbConnection connection = _database.Open();
        Dictionary<string, byte[]> byZone = new Dictionary<string, byte[]>();

        foreach (Row row in connection.Query<Row>("select zone as Zone, cells as Cells from player_discovery where player_id = @playerId;", new { playerId }))
        {
            byZone[row.Zone] = row.Cells;
        }

        return byZone;
    }

    public void Save(Guid playerId, string zone, byte[] cells)
    {
        using IDbConnection connection = _database.Open();
        connection.Execute(
            @"insert into player_discovery (player_id, zone, cells) values (@playerId, @zone, @cells)
              on conflict (player_id, zone) do update set cells = excluded.cells;",
            new { playerId, zone, cells });
    }

    private class Row
    {
        public string Zone { get; set; } = "";
        public byte[] Cells { get; set; } = Array.Empty<byte>();
    }
}
