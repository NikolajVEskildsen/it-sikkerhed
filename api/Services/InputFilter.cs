using System.Text.RegularExpressions;

namespace api.Services;

/// <summary>
/// Lille, selvstændig filtreringskomponent til den tredje variant
/// ("usikker med inputfiltrering").
///
/// BEVIDST BEGRÆNSET blacklist: fjerner kun de HELE ord <c>OR</c> og <c>UNION</c>
/// (uafhængigt af store/små bogstaver). Ordgrænser (<c>\b</c>) gør, at bogstaver
/// inde i almindelige ord bevares — fx <c>or</c> i "password".
///
/// Filteret fjerner IKKE specialtegn som <c>'</c> eller kommentaren <c>--</c>.
/// Derfor er det bevisligt omgåeligt: input bliver stadig en del af SQL-sætningen,
/// og angreb der ikke bruger netop ordene OR/UNION (fx <c>admin' --</c>) slipper
/// uændret igennem. Pointen er, at man ved blacklisting skal "jagte" alle ulovlige
/// tegn/ord — mens parameterisering fjerner selve årsagen.
/// </summary>
public class InputFilter
{
    // Kun de hele ord OR og UNION. \b = ordgrænse → "or" i "password" bevares.
    private static readonly Regex Blacklist =
        new(@"\b(?:OR|UNION)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Menneskelæsbar beskrivelse af reglen (vises i UI / README).</summary>
    public const string RuleDescription =
        "Blacklist (bevidst begrænset): fjerner kun de hele ord \"OR\" og \"UNION\" " +
        "(uafhængigt af store/små bogstaver). Specialtegn som ' og -- filtreres IKKE.";

    /// <summary>
    /// Fjerner de blacklistede ord fra <paramref name="input"/> og returnerer
    /// både det oprindelige og det filtrerede input, så kaldet kan vise præcist
    /// hvad der blev sendt videre til SQL-konstruktionen.
    /// </summary>
    public FilterResult Clean(string? input)
    {
        var original = input ?? "";
        var filtered = Blacklist.Replace(original, "");
        return new FilterResult(original, filtered, !string.Equals(original, filtered, StringComparison.Ordinal));
    }
}

/// <summary>Resultatet af en filtrering: hvad kom ind, hvad gik videre, og blev det ændret.</summary>
public readonly record struct FilterResult(string Original, string Filtered, bool Changed);
