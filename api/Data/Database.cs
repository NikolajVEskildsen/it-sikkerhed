using Microsoft.Data.SqlClient;
using api.Models;

namespace api.Data;

/// <summary>
/// Model-/datalag: taler med MSSQL. Det kender ikke de konkrete demo-forespørgsler
/// (de ligger i frontend) — det udfører bare den SQL det får.
/// Bindes <paramref name="parameters"/> med, er input data; ellers er det ren streng.
/// </summary>
public class Database
{
    private readonly string _conn;

    public Database(IConfiguration config) =>
        _conn = config["DB_CONNECTION"]
            ?? "Server=localhost,1433;Database=SqliDemo;User Id=sa;Password=Str0ng!Passw0rd;TrustServerCertificate=true";

    public QueryResult Execute(string sql, Dictionary<string, string>? parameters)
    {
        try
        {
            using var c = new SqlConnection(_conn);
            c.Open();
            using var cmd = new SqlCommand(sql, c);
            if (parameters != null)
                foreach (var (name, value) in parameters)
                    cmd.Parameters.AddWithValue(name, value);

            using var reader = cmd.ExecuteReader();
            var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
            var rows = new List<string[]>();
            while (reader.Read())
            {
                var row = new string[reader.FieldCount];
                for (var i = 0; i < reader.FieldCount; i++)
                    row[i] = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString() ?? "";
                rows.Add(row);
            }
            return new QueryResult { Columns = columns, Rows = rows };
        }
        catch (SqlException ex)
        {
            return new QueryResult { Error = ex.Message };
        }
    }
}
