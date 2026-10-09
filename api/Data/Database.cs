using Microsoft.Data.SqlClient;
using api.Models;

namespace api.Data;

/// <summary>
/// Model-/datalag. To måder at tale med databasen på:
///   - <see cref="ExecuteRaw"/>:   SQL-strengen køres som den er (den sårbare vej).
///   - <see cref="ExecuteParam"/>: input bindes som parametre (den korrekte vej).
/// </summary>
public class Database
{
    private readonly string _conn;

    public Database(IConfiguration config) =>
        _conn = config["DB_CONNECTION"]
            ?? "Server=localhost,1433;Database=SqliDemo;User Id=sa;Password=Str0ng!Passw0rd;TrustServerCertificate=true";

    /// <summary>SÅRBAR vej: kør strengen råt — input kan være blevet til kode.</summary>
    public QueryResult ExecuteRaw(string sql) => Run(sql, null, null);

    /// <summary>SIKKER vej: bind input som parametre — input forbliver data.</summary>
    public QueryResult ExecuteParam(string sql, Dictionary<string, string> parameters)
        => Run(sql, parameters, string.Join(", ", parameters.Select(p => $"{p.Key} = '{p.Value}'")));

    private QueryResult Run(string sql, Dictionary<string, string>? parameters, string? paramsDisplay)
    {
        var result = new QueryResult { Sql = sql, Parameters = paramsDisplay };
        try
        {
            using var c = new SqlConnection(_conn);
            c.Open();
            using var cmd = new SqlCommand(sql, c);
            if (parameters != null)
                foreach (var (name, value) in parameters)
                    cmd.Parameters.AddWithValue(name, value);

            using var reader = cmd.ExecuteReader();
            result.Columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
            var rows = new List<string[]>();
            while (reader.Read())
            {
                var row = new string[reader.FieldCount];
                for (var i = 0; i < reader.FieldCount; i++)
                    row[i] = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString() ?? "";
                rows.Add(row);
            }
            result.Rows = rows;
        }
        catch (SqlException ex)
        {
            result.Error = ex.Message;
        }
        return result;
    }
}
