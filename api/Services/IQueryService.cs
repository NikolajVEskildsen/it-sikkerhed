using api.Models;

namespace api.Services;

public interface IQueryService
{
    QueryResult Login(string username, string password, bool secure);
    QueryResult Search(string term, bool secure);
}
