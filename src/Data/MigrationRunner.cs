namespace MmoGame3d.Data;

using System.Data;
using System.Reflection;
using Dapper;

/// <summary>
/// Brings the schema up to date. Migrations are plain SQL files embedded in this
/// assembly and run once each, in name order, so a name starts with its sequence
/// number. Each runs in its own transaction with its record, so a failed one leaves
/// no half-applied schema behind.
/// </summary>
public static class MigrationRunner
{
    private const string ResourcePrefix = "MmoGame3d.Data.Migrations.";

    public static IReadOnlyList<string> Run(Database database)
    {
        List<string> applied = new List<string>();

        using IDbConnection connection = database.Open();
        connection.Execute(
            @"create table if not exists schema_migrations (
                  name text primary key,
                  applied_at timestamptz not null default now()
              );");

        HashSet<string> done = new HashSet<string>(connection.Query<string>("select name from schema_migrations;"));

        foreach (string name in MigrationNames())
        {
            if (done.Contains(name))
            {
                continue;
            }

            using IDbTransaction transaction = connection.BeginTransaction();
            connection.Execute(ReadMigration(name), transaction: transaction);
            connection.Execute("insert into schema_migrations (name) values (@name);", new { name }, transaction);
            transaction.Commit();
            applied.Add(name);
        }

        return applied;
    }

    public static List<string> MigrationNames()
    {
        List<string> names = new List<string>();

        foreach (string resource in typeof(MigrationRunner).Assembly.GetManifestResourceNames())
        {
            if (resource.StartsWith(ResourcePrefix, StringComparison.Ordinal) && resource.EndsWith(".sql", StringComparison.Ordinal))
            {
                names.Add(resource.Substring(ResourcePrefix.Length));
            }
        }

        names.Sort(StringComparer.Ordinal);
        return names;
    }

    private static string ReadMigration(string name)
    {
        Assembly assembly = typeof(MigrationRunner).Assembly;
        using Stream stream = assembly.GetManifestResourceStream(ResourcePrefix + name)!;
        using StreamReader reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
