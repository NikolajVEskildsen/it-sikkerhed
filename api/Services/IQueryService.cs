using api.Models;

namespace api.Services;

public interface IQueryService
{
    QueryResult Execute(QueryRequest request);
}
