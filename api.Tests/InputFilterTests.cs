using api.Services;

namespace api.Tests;

/// <summary>
/// Enhedstests for den begrænsede blacklist (<see cref="InputFilter"/>).
/// Disse kræver INGEN database.
/// </summary>
public class InputFilterTests
{
    private readonly InputFilter _filter = new();

    [Fact]
    public void OrBaseret_angreb_bliver_transformeret()
    {
        // Eksempel A, password-feltet: "x' OR '1'='1"
        var r = _filter.Clean("x' OR '1'='1");

        Assert.True(r.Changed);
        Assert.DoesNotContain("OR", r.Filtered, StringComparison.OrdinalIgnoreCase);
        // Det hele ord OR fjernes; resten (inkl. ' ) bevares.
        Assert.Equal("x'  '1'='1", r.Filtered);
    }

    [Fact]
    public void Union_bliver_fjernet_case_insensitivt()
    {
        var r = _filter.Clean("' uNiOn SELECT username, password FROM users --");

        Assert.True(r.Changed);
        Assert.DoesNotContain("union", r.Filtered, StringComparison.OrdinalIgnoreCase);
        // SELECT, kommentar og specialtegn bevares — kun ordet UNION ryger.
        Assert.Contains("SELECT username, password", r.Filtered);
        Assert.Contains("--", r.Filtered);
    }

    [Fact]
    public void AdminKommentar_bypass_slipper_uaendret_igennem()
    {
        // Eksempel B: "admin' --" indeholder hverken OR eller UNION.
        var r = _filter.Clean("admin' --");

        Assert.False(r.Changed);
        Assert.Equal("admin' --", r.Filtered);
    }

    [Theory]
    [InlineData("password")]      // "or" inde i et ord
    [InlineData("doctor")]        // "or" inde i et ord
    [InlineData("corn")]          // "or" inde i et ord
    [InlineData("reunion")]       // "union" inde i et ord
    [InlineData("mentor")]        // "or" i slutningen af et ord
    public void Bogstaver_inde_i_almindelige_ord_bevares(string input)
    {
        var r = _filter.Clean(input);

        Assert.False(r.Changed);
        Assert.Equal(input, r.Filtered);
    }

    [Theory]
    [InlineData("a OR b", "a  b")]
    [InlineData("a or b", "a  b")]
    [InlineData("a Or b", "a  b")]
    [InlineData("UNION", "")]
    [InlineData("1=1 OR 2=2", "1=1  2=2")]
    public void Hele_ord_OR_og_UNION_fjernes(string input, string expected)
    {
        Assert.Equal(expected, _filter.Clean(input).Filtered);
    }

    [Fact]
    public void Harmlos_input_er_uaendret()
    {
        var r = _filter.Clean("mette");
        Assert.False(r.Changed);
        Assert.Equal("mette", r.Filtered);
    }

    [Fact]
    public void Null_behandles_som_tom_streng()
    {
        var r = _filter.Clean(null);
        Assert.False(r.Changed);
        Assert.Equal("", r.Filtered);
    }

    [Fact]
    public void Original_bevares_i_resultatet()
    {
        var r = _filter.Clean("x' OR '1'='1");
        Assert.Equal("x' OR '1'='1", r.Original);
    }
}
