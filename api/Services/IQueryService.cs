using api.Models;

namespace api.Services;

public interface IQueryService
{
    QueryResult SecureLogin(string username, string password);
    QueryResult UnsecureLogin(string username, string password);
    QueryResult SecureSearch(string term);
    QueryResult UnsecureSearch(string term);
}
