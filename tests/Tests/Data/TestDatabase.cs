namespace MmoGame3d.Tests.Data;

using System.Data;
using Dapper;
using MmoGame3d.Data;

/// <summary>
/// A real Postgres database for the store tests, wiped and migrated once per test class.
/// It is mmo3d_test on the local server unless MMO3D_TEST_DB says otherwise. Never point
/// it at the game's own database: the wipe drops everything.
/// </summary>
public class TestDatabase
{
    private const string DefaultConnectionString = "Host=localhost;Port=5432;Database=mmo3d_test;Username=mmo;Password=mmo;GSS Encryption Mode=Disable";

    public TestDatabase()
    {
        string? fromEnvironment = Environment.GetEnvironmentVariable("MMO3D_TEST_DB");
        Database = new Database(string.IsNullOrEmpty(fromEnvironment) ? DefaultConnectionString : fromEnvironment);

        using (IDbConnection connection = Database.Open())
        {
            connection.Execute("drop schema public cascade; create schema public;");
        }

        MigrationRunner.Run(Database);
    }

    public Database Database { get; }
}

// Test classes that share the database run one at a time, so one class's wipe never
// lands in the middle of another's test.
[CollectionDefinition(Name)]
public class DatabaseCollection : ICollectionFixture<TestDatabase>
{
    public const string Name = "Database";
}
