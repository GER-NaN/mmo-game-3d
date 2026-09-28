namespace MmoGame3d.Data.Gardening;

using System.Data;
using Dapper;

// House plants and their history. A plant is written once and never changed; what
// happens to it later is added to its history.
public class PlantStore
{
    private readonly Database _database;

    public PlantStore(Database database)
    {
        _database = database;
    }

    // The new plant's number, with its first history line written in the same
    // transaction, so no plant exists without its provenance.
    public long Create(Guid creatorId, string creatorName, string name, string design, string firstEvent)
    {
        using IDbConnection connection = _database.Open();
        using IDbTransaction transaction = connection.BeginTransaction();
        long id = connection.ExecuteScalar<long>(
            @"insert into plants (created_by, creator_name, name, design) values (@creatorId, @creatorName, @name, @design) returning id;",
            new { creatorId, creatorName, name, design },
            transaction);
        connection.Execute("insert into plant_history (plant_id, event) values (@id, @firstEvent);", new { id, firstEvent }, transaction);
        transaction.Commit();
        return id;
    }

    public void AddEvent(long plantId, string text)
    {
        using IDbConnection connection = _database.Open();
        connection.Execute("insert into plant_history (plant_id, event) values (@plantId, @text);", new { plantId, text });
    }

    // The newest plants, newest first: what stands on display.
    public List<PlantRecord> Newest(int count)
    {
        using IDbConnection connection = _database.Open();
        return connection.Query<PlantRecord>(Select + " order by id desc limit @count;", new { count }).ToList();
    }

    public PlantRecord? Get(long id)
    {
        using IDbConnection connection = _database.Open();
        PlantRecord? plant = connection.QuerySingleOrDefault<PlantRecord>(Select + " where id = @id;", new { id });

        if (plant != null)
        {
            plant.History = connection.Query<PlantEvent>(
                "select at as At, event as Text from plant_history where plant_id = @id order by id;",
                new { id }).ToList();
        }

        return plant;
    }

    private const string Select =
        "select id as Id, created_by as CreatedBy, creator_name as CreatorName, name as Name, design as Design, created_at as CreatedAt from plants";
}
