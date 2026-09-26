namespace MmoGame3d.Data.Social;

using System.Data;
using Dapper;
using MmoGame3d.Rules.Social;

// Friends and ignored players. The names come from the players table, so a friend's
// list shows their current name.
public class ContactStore
{
    private readonly Database _database;

    public ContactStore(Database database)
    {
        _database = database;
    }

    public Contacts Load(Guid playerId)
    {
        using IDbConnection connection = _database.Open();
        Contacts contacts = new Contacts();

        foreach (Row row in connection.Query<Row>(
            @"select c.contact_id as ContactId, p.display_name as Name, c.ignored as Ignored
              from contacts c join players p on p.id = c.contact_id
              where c.player_id = @playerId;",
            new { playerId }))
        {
            contacts.Load(row.ContactId, row.Name, row.Ignored);
        }

        return contacts;
    }

    // A friend or an ignore; a row already there changes list.
    public void Set(Guid playerId, Guid contactId, bool ignored)
    {
        using IDbConnection connection = _database.Open();
        connection.Execute(
            @"insert into contacts (player_id, contact_id, ignored) values (@playerId, @contactId, @ignored)
              on conflict (player_id, contact_id) do update set ignored = excluded.ignored;",
            new { playerId, contactId, ignored });
    }

    public void Remove(Guid playerId, Guid contactId)
    {
        using IDbConnection connection = _database.Open();
        connection.Execute("delete from contacts where player_id = @playerId and contact_id = @contactId;", new { playerId, contactId });
    }

    private class Row
    {
        public Guid ContactId { get; set; }
        public string Name { get; set; } = "";
        public bool Ignored { get; set; }
    }
}
