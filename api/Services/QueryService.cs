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

    public QueryResult SecureLogin(string username, string password)
    {
        // Sikker: input sættes som parameter.
        const string sql = "SELECT id, username, email FROM users " +
                            "WHERE username = @username AND password = @password";
        return _db.ExecuteParam(sql, new() { ["@username"] = username, ["@password"] = password });   
    }

    public QueryResult UnsecureLogin(string username, string password)
    {
        // SÅRBAR: input sættes direkte ind i strengen.
        var raw = "SELECT id, username, email FROM users " +
                  $"WHERE username = '{username}' AND password = '{password}'";
        return _db.ExecuteRaw(raw);
    }

    public QueryResult SecureSearch(string term)
    {
        // Sikker: input sættes som parameter.
        const string sql = "SELECT username, email FROM users " +
                            "WHERE username LIKE '%' + @term + '%'";
        return _db.ExecuteParam(sql, new() { ["@term"] = term });
    }

    public QueryResult UnsecureSearch(string term)
    {
        // SÅRBAR: input sættes direkte ind i strengen.
        var raw = "SELECT username, email FROM users " +
                  $"WHERE username LIKE '%{term}%'";
        return _db.ExecuteRaw(raw);
    }
}
