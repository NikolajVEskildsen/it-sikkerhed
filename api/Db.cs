using Microsoft.Data.SqlClient;

namespace api;

/// <summary>
/// Dataadgang mod MSSQL til SQL Injection-demoen.
///
/// To forespørgselsveje ligger side om side:
///   - "Vulnerable"-metoderne bygger SQL ved streng-sammensætning (data blandes med kode).
///   - "Secure"-metoderne bruger parameteriserede forespørgsler (data holdes adskilt fra kode).
///
/// ADVARSEL: De sårbare metoder er med vilje usikre. De er kun til undervisning.
/// </summary>
public sealed class Db
{
    private readonly string _conn;

    public Db(string connectionString) => _conn = connectionString;

    // ---- Opsætning --------------------------------------------------------

    /// <summary>Opretter database + tabel og indsætter mock-data. Venter på at SQL Server er klar.</summary>
    public void EnsureSeeded()
    {
        var b = new SqlConnectionStringBuilder(_conn);
        var targetDb = b.InitialCatalog;

        // Forbind til master for at kunne oprette selve databasen.
        var masterCs = new SqlConnectionStringBuilder(_conn) { InitialCatalog = "master" }.ConnectionString;

        // SQL Server-containeren er typisk 10-30 sek om at blive klar.
        WaitForServer(masterCs);

        Exec(masterCs, $"IF DB_ID('{targetDb}') IS NULL CREATE DATABASE [{targetDb}];");
        Exec(_conn, """
            IF OBJECT_ID('dbo.users', 'U') IS NOT NULL DROP TABLE dbo.users;
            CREATE TABLE dbo.users (
                id       INT IDENTITY PRIMARY KEY,
                username NVARCHAR(50)  NOT NULL,
                password NVARCHAR(100) NOT NULL,
                role     NVARCHAR(20)  NOT NULL,
                email    NVARCHAR(100) NOT NULL,
                secret   NVARCHAR(200) NOT NULL
            );
            INSERT INTO dbo.users (username, password, role, email, secret) VALUES
                ('admin', 'S3cr3t!Admin', 'admin', 'admin@acme.dk', 'Masternøgle: ACME-ROOT-2026'),
                ('mette', 'sommer2025',   'user',  'mette@acme.dk', 'Privat: lønseddel-marts.pdf'),
                ('jonas', 'hunden-max',   'user',  'jonas@acme.dk', 'Privat: kreditkort slutter 4417'),
                ('sofie', 'P@ssw0rd',     'user',  'sofie@acme.dk', 'Privat: sygesikringsnr 120398'),
                ('lars',  'qwerty123',    'user',  'lars@acme.dk',  'Privat: adgangskode til router');
            """);
    }

    private static void WaitForServer(string masterCs)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var c = new SqlConnection(masterCs);
                c.Open();
                return;
            }
            catch (SqlException) when (attempt < 30)
            {
                Thread.Sleep(2000); // ponytail: fast 2s-interval retry, nok til container-opstart
            }
        }
    }

    private static void Exec(string cs, string sql)
    {
        using var c = new SqlConnection(cs);
        c.Open();
        using var cmd = new SqlCommand(sql, c);
        cmd.ExecuteNonQuery();
    }

    // ---- LOGIN: auth-bypass demo ------------------------------------------

    /// <summary>SÅRBAR: bruger-input sættes direkte ind i SQL-strengen.</summary>
    public QueryResult VulnerableLogin(string username, string password)
    {
        var sql = "SELECT id, username, role, email FROM users " +
                  $"WHERE username = '{username}' AND password = '{password}'";
        return RunRaw(sql);
    }

    /// <summary>SIKKER: bruger-input bindes som parametre, aldrig som kode.</summary>
    public QueryResult SecureLogin(string username, string password)
    {
        const string sql = "SELECT id, username, role, email FROM users " +
                           "WHERE username = @username AND password = @password";
        return RunParam(sql, ("@username", username), ("@password", password));
    }

    // ---- SØGNING: data-exfiltration (UNION) demo --------------------------

    /// <summary>SÅRBAR: søgeterm sættes direkte ind i SQL-strengen.</summary>
    public QueryResult VulnerableSearch(string term)
    {
        var sql = "SELECT username, email FROM users " +
                  $"WHERE username LIKE '%{term}%'";
        return RunRaw(sql);
    }

    /// <summary>SIKKER: søgeterm bindes som parameter.</summary>
    public QueryResult SecureSearch(string term)
    {
        const string sql = "SELECT username, email FROM users " +
                           "WHERE username LIKE '%' + @term + '%'";
        return RunParam(sql, ("@term", term));
    }

    // ---- Udførsel ---------------------------------------------------------

    private QueryResult RunRaw(string sql)
    {
        using var c = new SqlConnection(_conn);
        c.Open();
        using var cmd = new SqlCommand(sql, c);
        return Read(sql, null, cmd);
    }

    private QueryResult RunParam(string sql, params (string name, object value)[] ps)
    {
        using var c = new SqlConnection(_conn);
        c.Open();
        using var cmd = new SqlCommand(sql, c);
        foreach (var (name, value) in ps)
            cmd.Parameters.AddWithValue(name, value);
        var bound = string.Join(", ", ps.Select(p => $"{p.name} = '{p.value}'"));
        return Read(sql, bound, cmd);
    }

    private static QueryResult Read(string sql, string? boundParams, SqlCommand cmd)
    {
        try
        {
            using var reader = cmd.ExecuteReader();
            var columns = Enumerable.Range(0, reader.FieldCount)
                                    .Select(reader.GetName).ToArray();
            var rows = new List<string[]>();
            while (reader.Read())
            {
                var row = new string[reader.FieldCount];
                for (var i = 0; i < reader.FieldCount; i++)
                    row[i] = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString() ?? "";
                rows.Add(row);
            }
            return new QueryResult(sql, boundParams, columns, rows, null);
        }
        catch (SqlException ex)
        {
            return new QueryResult(sql, boundParams, [], [], ex.Message);
        }
    }
}

/// <param name="Sql">Den SQL der faktisk blev sendt til databasen.</param>
/// <param name="BoundParams">Bundne parametre (kun for sikre forespørgsler), ellers null.</param>
public sealed record QueryResult(
    string Sql,
    string? BoundParams,
    string[] Columns,
    List<string[]> Rows,
    string? Error);
