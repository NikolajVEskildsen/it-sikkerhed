using Microsoft.Data.SqlClient;

namespace api.Data;

/// <summary>Opretter database + tabel og indsætter mock-data ved opstart.</summary>
public static class DbSeeder
{
    public static void Seed(string conn)
    {
        var target = new SqlConnectionStringBuilder(conn).InitialCatalog;
        var master = new SqlConnectionStringBuilder(conn) { InitialCatalog = "master" }.ConnectionString;

        WaitForServer(master); // SQL Server-containeren er 10-30 sek om at blive klar

        Exec(master, $"IF DB_ID('{target}') IS NULL CREATE DATABASE [{target}];");
        Exec(conn, """
            IF OBJECT_ID('dbo.users', 'U') IS NOT NULL DROP TABLE dbo.users;
            CREATE TABLE dbo.users (
                id       INT IDENTITY PRIMARY KEY,
                username NVARCHAR(50)  NOT NULL,
                password NVARCHAR(100) NOT NULL,
                email    NVARCHAR(100) NOT NULL
            );
            INSERT INTO dbo.users (username, password, email) VALUES
                ('admin', 'admin123',   'admin@acme.dk'),
                ('mette', 'sommer2025', 'mette@acme.dk'),
                ('jonas', 'hunden-max', 'jonas@acme.dk');
            """);
    }

    private static void WaitForServer(string master)
    {
        for (var attempt = 1; ; attempt++)
        {
            try { using var c = new SqlConnection(master); c.Open(); return; }
            catch (SqlException) when (attempt < 30) { Thread.Sleep(2000); }
        }
    }

    private static void Exec(string cs, string sql)
    {
        using var c = new SqlConnection(cs);
        c.Open();
        using var cmd = new SqlCommand(sql, c);
        cmd.ExecuteNonQuery();
    }
}
