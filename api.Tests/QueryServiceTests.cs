using api.Data;
using api.Services;
using Microsoft.Extensions.Configuration;

namespace api.Tests;

/// <summary>
/// Tests af SQL-konstruktionen og filter-koblingen i <see cref="QueryService"/>.
///
/// Databasen peges bevidst på en port der afvises med det samme (Connect Timeout=1),
/// så ingen kørende MSSQL er nødvendig: <see cref="QueryResult.Sql"/> (og
/// <see cref="QueryResult.Parameters"/> / <see cref="QueryResult.Filter"/>) sættes
/// FØR forbindelsen åbnes. Rækker/bypass mod en RIGTIG database verificeres separat
/// via scripts/verify-db.sh (se README).
/// </summary>
public class QueryServiceTests
{
    private static QueryService NewService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Port 9 (discard) -> forbindelse afvises med det samme.
                ["DB_CONNECTION"] = "Server=localhost,9;Database=none;User Id=sa;Password=x;TrustServerCertificate=true;Connect Timeout=1",
            })
            .Build();
        return new QueryService(new Database(config), new InputFilter());
    }

    // ---- Eksisterende adfærd uændret -------------------------------------

    [Fact]
    public void UnsecureLogin_bygger_raa_streng_med_input()
    {
        var r = NewService().UnsecureLogin("admin' --", "");
        Assert.Equal("SELECT id, username, email FROM users WHERE username = 'admin' --' AND password = ''", r.Sql);
        Assert.Null(r.Parameters);   // ingen parametre i den sårbare vej
        Assert.Null(r.Filter);       // ingen filtrering i version 1
    }

    [Fact]
    public void SecureLogin_bruger_parametre_ikke_raa_input()
    {
        var r = NewService().SecureLogin("admin' --", "secret");
        Assert.Contains("@username", r.Sql);
        Assert.Contains("@password", r.Sql);
        Assert.DoesNotContain("admin' --", r.Sql);     // input er IKKE i SQL-strengen
        Assert.NotNull(r.Parameters);                   // bundne parametre vises
        Assert.Contains("admin' --", r.Parameters!);    // input er data
        Assert.Null(r.Filter);                          // parameteriseret har ingen filtrering
    }

    [Fact]
    public void UnsecureSearch_og_SecureSearch_uaendret()
    {
        var svc = NewService();
        Assert.Equal("SELECT username, email FROM users WHERE username LIKE '%mette%'", svc.UnsecureSearch("mette").Sql);
        Assert.Contains("@term", svc.SecureSearch("mette").Sql);
    }

    // ---- Tredje variant: filtrering --------------------------------------

    [Fact]
    public void FilteredLogin_transformerer_OR_eksemplet_i_SQL()
    {
        var r = NewService().FilteredLogin("x", "x' OR '1'='1");

        // OR er fjernet, så det ryger IKKE med i SQL-strengen.
        Assert.DoesNotContain(" OR ", r.Sql);
        Assert.Contains("password = 'x'  '1'='1'", r.Sql);

        Assert.NotNull(r.Filter);
        var pass = r.Filter!.Fields.Single(f => f.Name == "Adgangskode");
        Assert.True(pass.Changed);
        Assert.Equal("x' OR '1'='1", pass.Original);
        Assert.Equal("x'  '1'='1", pass.Filtered);
    }

    [Fact]
    public void FilteredLogin_admin_kommentar_bypass_er_identisk_med_usikker()
    {
        var svc = NewService();
        var unsecure = svc.UnsecureLogin("admin' --", "");
        var filtered = svc.FilteredLogin("admin' --", "");

        // admin' -- indeholder hverken OR eller UNION -> samme SQL som den rå version.
        Assert.Equal(unsecure.Sql, filtered.Sql);
        Assert.NotNull(filtered.Filter);
        Assert.All(filtered.Filter!.Fields, f => Assert.False(f.Changed));
    }

    [Fact]
    public void Harmlost_input_giver_identisk_SQL_i_begge_usikre_versioner()
    {
        var svc = NewService();
        Assert.Equal(svc.UnsecureLogin("mette", "x").Sql, svc.FilteredLogin("mette", "x").Sql);
        Assert.Equal(svc.UnsecureSearch("mette").Sql, svc.FilteredSearch("mette").Sql);
    }

    [Fact]
    public void FilteredSearch_fjerner_UNION()
    {
        var r = NewService().FilteredSearch("' UNION SELECT username, password FROM users --");

        Assert.DoesNotContain("UNION", r.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(r.Filter);
        Assert.True(r.Filter!.Fields.Single().Changed);
    }

    [Fact]
    public void FilteredLogin_angiver_reglen_fra_InputFilter()
    {
        var r = NewService().FilteredLogin("a", "b");
        Assert.Equal(InputFilter.RuleDescription, r.Filter!.Rule);
    }
}
