using api.Data;
using api.Models;

namespace api.Services;

/// <summary>
/// Logiklaget. Her ligger de to udgaver af hver forespørgsel.
/// <c>secure</c>-flaget afgør om input bliver en del af SQL-strengen (sårbar)
/// eller bindes som parameter (sikker).
/// </summary>
public class QueryService : IQueryService
{
    private readonly Database _db;

    public QueryService(Database db) => _db = db;

    public QueryResult Login(string username, string password, bool secure)
    {
        if (secure)
        {
            const string sql = "SELECT id, username, email FROM users " +
                               "WHERE username = @username AND password = @password";
            return _db.ExecuteParam(sql, new() { ["@username"] = username, ["@password"] = password });
        }

        // SÅRBAR: input sættes direkte ind i strengen.
        var raw = "SELECT id, username, email FROM users " +
                  $"WHERE username = '{username}' AND password = '{password}'";
        return _db.ExecuteRaw(raw);
    }

    public QueryResult Search(string term, bool secure)
    {
        if (secure)
        {
            const string sql = "SELECT username, email FROM users " +
                               "WHERE username LIKE '%' + @term + '%'";
            return _db.ExecuteParam(sql, new() { ["@term"] = term });
        }

        // SÅRBAR: input sættes direkte ind i strengen.
        var raw = "SELECT username, email FROM users " +
                  $"WHERE username LIKE '%{term}%'";
        return _db.ExecuteRaw(raw);
    }
}
