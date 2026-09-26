namespace MmoGame3d.Data.Progress;

using System.Data;
using Dapper;

// The achievements each player has earned.
public class AchievementStore
{
    private readonly Database _database;

    public AchievementStore(Database database)
    {
        _database = database;
    }

    public HashSet<string> Load(Guid playerId)
    {
        using IDbConnection connection = _database.Open();
        return new HashSet<string>(connection.Query<string>(
            "select achievement from player_achievements where player_id = @playerId;", new { playerId }));
    }

    // Earning one twice changes nothing.
    public void Grant(Guid playerId, string achievement)
    {
        using IDbConnection connection = _database.Open();
        connection.Execute(
            "insert into player_achievements (player_id, achievement) values (@playerId, @achievement) on conflict do nothing;",
            new { playerId, achievement });
    }
}
