namespace MmoGame3d.Data.Scores;

using System.Data;
using Dapper;

// Objective results and their leaderboards. A score is better lower (guesses used) or
// higher (points), as the objective says; a tie goes to the faster.
public class ScoreStore
{
    private readonly Database _database;

    public ScoreStore(Database database)
    {
        _database = database;
    }

    public void Add(string objective, Guid playerId, string playerName, int score, double seconds)
    {
        using IDbConnection connection = _database.Open();
        connection.Execute(
            "insert into scores (objective, player_id, player_name, score, seconds) values (@objective, @playerId, @playerName, @score, @seconds);",
            new { objective, playerId, playerName, score, seconds });
    }

    // Each player's best, best first.
    public List<ScoreRecord> Top(string objective, bool lowerIsBetter, int count)
    {
        string order = Order(lowerIsBetter);
        using IDbConnection connection = _database.Open();
        return connection.Query<ScoreRecord>(
            "select * from (select distinct on (player_id) player_id as PlayerId, player_name as PlayerName, score as Score, seconds as Seconds " +
            "from scores where objective = @objective order by player_id, " + order + ") best order by " + order + " limit @count;",
            new { objective, count }).ToList();
    }

    public ScoreRecord? Best(string objective, Guid playerId, bool lowerIsBetter)
    {
        using IDbConnection connection = _database.Open();
        return connection.QueryFirstOrDefault<ScoreRecord>(
            "select player_id as PlayerId, player_name as PlayerName, score as Score, seconds as Seconds " +
            "from scores where objective = @objective and player_id = @playerId order by " + Order(lowerIsBetter) + " limit 1;",
            new { objective, playerId });
    }

    // Fixed text only, never from a caller: the order is not a parameter in SQL.
    private static string Order(bool lowerIsBetter)
    {
        return lowerIsBetter ? "score asc, seconds asc" : "score desc, seconds asc";
    }
}
