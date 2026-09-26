namespace MmoGame3d.Data.Players;

using System.Data;
using Dapper;
using MmoGame3d.Rules.Items;

public class PlayerStore
{
    private const string SelectColumns =
        @"id as PlayerId, account_id as AccountId, display_name as DisplayName, zone as Zone,
          position_x as PositionX, position_y as PositionY, position_z as PositionZ, yaw as Yaw,
          dollars as Dollars";

    private readonly Database _database;

    public PlayerStore(Database database)
    {
        _database = database;
    }

    /// <summary>
    /// The account's player with what they carry, made from newPlayer when the account
    /// has none. A returning player reads their own saved row: the name and spawn passed
    /// in are only for a new one, since a display name is chosen once and never renamed.
    /// </summary>
    public PlayerRecord GetOrCreate(PlayerRecord newPlayer, out bool created)
    {
        using IDbConnection connection = _database.Open();

        int inserted = connection.Execute(
            @"insert into players (id, account_id, display_name, zone, position_x, position_y, position_z, yaw, dollars)
              values (@PlayerId, @AccountId, @DisplayName, @Zone, @PositionX, @PositionY, @PositionZ, @Yaw, @Dollars)
              on conflict (account_id) do nothing;",
            newPlayer);

        created = inserted == 1;

        PlayerRecord player = connection.QuerySingle<PlayerRecord>(
            "select " + SelectColumns + " from players where account_id = @AccountId;",
            new { newPlayer.AccountId });

        IEnumerable<StackRow> rows = connection.Query<StackRow>(
            "select item_type as ItemType, tier as Tier, quantity as Quantity from inventory_stacks where player_id = @PlayerId order by item_type, tier;",
            new { player.PlayerId });

        foreach (StackRow row in rows)
        {
            player.Stacks.Add(new ItemStack(Enum.Parse<ItemType>(row.ItemType), Enum.Parse<ItemTier>(row.Tier), row.Quantity));
        }

        return player;
    }

    // The row and the stacks in one transaction, so a player is never saved with where
    // they stood from one moment and what they held from another. Stacks are replaced
    // whole: a bag is small, and a diff would be more code for nothing.
    public void Save(PlayerRecord player)
    {
        using IDbConnection connection = _database.Open();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            @"update players
              set zone = @Zone, position_x = @PositionX, position_y = @PositionY, position_z = @PositionZ,
                  yaw = @Yaw, dollars = @Dollars, saved_at = now()
              where id = @PlayerId;",
            player,
            transaction);

        connection.Execute("delete from inventory_stacks where player_id = @PlayerId;", new { player.PlayerId }, transaction);

        foreach (ItemStack stack in player.Stacks)
        {
            connection.Execute(
                "insert into inventory_stacks (player_id, item_type, tier, quantity) values (@PlayerId, @ItemType, @Tier, @Quantity);",
                new { player.PlayerId, ItemType = stack.Type.ToString(), Tier = stack.Tier.ToString(), stack.Quantity },
                transaction);
        }

        transaction.Commit();
    }

    private class StackRow
    {
        public string ItemType { get; set; } = "";
        public string Tier { get; set; } = "";
        public int Quantity { get; set; }
    }
}
