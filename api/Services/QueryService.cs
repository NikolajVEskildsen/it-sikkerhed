using api.Data;
using api.Models;

namespace api.Services;

/// <summary>
/// Logiklaget. Her ligger de tre udgaver af hver forespørgsel:
///   1. Unsecure*  — input sættes direkte ind i SQL-strengen (sårbar).
///   2. Filtered*  — samme sårbare SQL-konstruktion, men input køres FØRST gennem
///                   <see cref="InputFilter"/> (bevidst begrænset blacklist).
///   3. Secure*    — input bindes som parameter (sikker).
///
/// Den filtrerede variant genbruger bevidst de eksisterende Unsecure*-metoder og
/// sender blot de filtrerede værdier videre — så SQL-konstruktion og eksekvering
/// (ExecuteRaw) er NØJAGTIG den samme som i den oprindelige sårbare version.
/// </summary>
public class QueryService : IQueryService
{
    private readonly Database _db;
    private readonly InputFilter _filter;

    public QueryService(Database db, InputFilter filter)
    {
        _db = db;
        _filter = filter;
    }

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

    public QueryResult FilteredLogin(string username, string password)
    {
        // Filtrér FØR SQL bygges — kør derefter NØJAGTIG samme sårbare vej.
        var u = _filter.Clean(username);
        var p = _filter.Clean(password);

        var result = UnsecureLogin(u.Filtered, p.Filtered);
        result.Filter = new FilterInfo
        {
            Rule = InputFilter.RuleDescription,
            Fields =
            [
                new FilterField { Name = "Brugernavn",  Original = u.Original, Filtered = u.Filtered, Changed = u.Changed },
                new FilterField { Name = "Adgangskode", Original = p.Original, Filtered = p.Filtered, Changed = p.Changed },
            ],
        };
        return result;
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

    public QueryResult FilteredSearch(string term)
    {
        // Filtrér FØR SQL bygges — kør derefter NØJAGTIG samme sårbare vej.
        var t = _filter.Clean(term);

        var result = UnsecureSearch(t.Filtered);
        result.Filter = new FilterInfo
        {
            Rule = InputFilter.RuleDescription,
            Fields =
            [
                new FilterField { Name = "Søgeterm", Original = t.Original, Filtered = t.Filtered, Changed = t.Changed },
            ],
        };
        return result;
    }
}
