namespace MmoGame3d.Data.Accounts;

using System.Data;
using Dapper;

public class AccountStore
{
    private readonly Database _database;

    public AccountStore(Database database)
    {
        _database = database;
    }

    // One statement, not a read then a write: two connects with the same new key at
    // once would otherwise both insert, and the unique index would reject one.
    public Guid GetOrCreate(Guid licenseKey)
    {
        using IDbConnection connection = _database.Open();

        connection.Execute(
            @"insert into accounts (id, license_key) values (@id, @licenseKey)
              on conflict (license_key) do nothing;",
            new { id = Guid.NewGuid(), licenseKey });

        return connection.QuerySingle<Guid>("select id from accounts where license_key = @licenseKey;", new { licenseKey });
    }
}
