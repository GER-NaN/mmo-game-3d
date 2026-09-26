namespace MmoGame3d.Data.Players;

using System.Data;
using Dapper;

public class PlayerStore
{
    private const string SelectColumns =
        @"id as PlayerId, account_id as AccountId, display_name as DisplayName, zone as Zone,
          position_x as PositionX, position_y as PositionY, position_z as PositionZ, yaw as Yaw";

    private readonly Database _database;

    public PlayerStore(Database database)
    {
        _database = database;
    }

    /// <summary>
    /// The account's player, made from newPlayer when the account has none. A returning
    /// player reads their own saved row: the name and spawn passed in are only for a new
    /// one, since a display name is chosen once and never renamed.
    /// </summary>
    public PlayerRecord GetOrCreate(PlayerRecord newPlayer, out bool created)
    {
        using IDbConnection connection = _database.Open();

        int inserted = connection.Execute(
            @"insert into players (id, account_id, display_name, zone, position_x, position_y, position_z, yaw)
              values (@PlayerId, @AccountId, @DisplayName, @Zone, @PositionX, @PositionY, @PositionZ, @Yaw)
              on conflict (account_id) do nothing;",
            newPlayer);

        created = inserted == 1;

        return connection.QuerySingle<PlayerRecord>(
            "select " + SelectColumns + " from players where account_id = @AccountId;",
            new { newPlayer.AccountId });
    }

    public void Save(PlayerRecord player)
    {
        using IDbConnection connection = _database.Open();

        connection.Execute(
            @"update players
              set zone = @Zone, position_x = @PositionX, position_y = @PositionY, position_z = @PositionZ,
                  yaw = @Yaw, saved_at = now()
              where id = @PlayerId;",
            player);
    }
}
