namespace MmoGame3d.Data;

using System.Data;
using Npgsql;

/// <summary>
/// Where the game's data lives. Every store opens a short-lived connection from here;
/// Npgsql pools them, so opening one per call is cheap.
/// </summary>
public class Database
{
    // GSS off because we authenticate with a password: otherwise Npgsql probes Kerberos
    // on every connect, and a machine without a Kerberos library fails the probe.
    public const string DefaultConnectionString = "Host=localhost;Port=5432;Database=mmo3d;Username=mmo;Password=mmo;GSS Encryption Mode=Disable";

    public Database(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public string ConnectionString { get; }

    public IDbConnection Open()
    {
        NpgsqlConnection connection = new NpgsqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }
}
