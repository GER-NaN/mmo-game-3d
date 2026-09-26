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

        IEnumerable<InstanceRow> instances = connection.Query<InstanceRow>(
            @"select id as Id, parent_id as ParentId, item_type as ItemType, tier as Tier, slot as Slot, charge as Charge
              from item_instances where player_id = @PlayerId;",
            new { player.PlayerId });

        foreach (InstanceRow row in instances)
        {
            player.Instances.Add(new ItemInstance(row.Id, Enum.Parse<ItemType>(row.ItemType), Enum.Parse<ItemTier>(row.Tier))
            {
                ParentId = row.ParentId,
                Slot = row.Slot == null ? null : Enum.Parse<SlotType>(row.Slot),
                Charge = row.Charge,
            });
        }

        player.Created = created;
        return player;
    }

    // The row, the stacks and the instances in one transaction, so a player is never
    // saved with where they stood from one moment and what they held from another. The
    // bag is replaced whole: it is small, and a diff would be more code for nothing.
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

        connection.Execute("delete from item_instances where player_id = @PlayerId;", new { player.PlayerId }, transaction);

        foreach (ItemInstance instance in player.Instances)
        {
            connection.Execute(
                @"insert into item_instances (id, player_id, parent_id, item_type, tier, slot, charge)
                  values (@Id, @PlayerId, @ParentId, @ItemType, @Tier, @Slot, @Charge);",
                new
                {
                    instance.Id,
                    player.PlayerId,
                    instance.ParentId,
                    ItemType = instance.Type.ToString(),
                    Tier = instance.Tier.ToString(),
                    Slot = instance.Slot?.ToString(),
                    instance.Charge,
                },
                transaction);
        }

        transaction.Commit();
    }

    private class InstanceRow
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        public string ItemType { get; set; } = "";
        public string Tier { get; set; } = "";
        public string? Slot { get; set; }
        public float? Charge { get; set; }
    }

    private class StackRow
    {
        public string ItemType { get; set; } = "";
        public string Tier { get; set; } = "";
        public int Quantity { get; set; }
    }
}
