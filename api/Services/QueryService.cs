using api.Data;
using api.Models;

namespace api.Services;

/// <summary>Logiklaget: tager forespørgslen fra controlleren og beder modellen udføre den.</summary>
public class QueryService : IQueryService
{
    private readonly Database _database;

    public QueryService(Database database) => _database = database;

    public QueryResult Execute(QueryRequest request)
        => _database.Execute(request.Sql, request.Parameters);
}
