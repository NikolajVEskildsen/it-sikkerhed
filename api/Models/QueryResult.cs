namespace api.Models;

/// <summary>Resultatet af en forespørgsel: hvilken SQL der kørte + kolonner/rækker, eller en fejl.</summary>
public class QueryResult
{
    public string Sql { get; set; } = "";
    public string? Parameters { get; set; }    // bundne parametre (kun parameteriseret vej), ellers null
    public string[] Columns { get; set; } = [];
    public List<string[]> Rows { get; set; } = [];
    public string? Error { get; set; }

    /// <summary>
    /// Filtreringsdetaljer — kun sat i den tredje variant ("usikker med inputfiltrering").
    /// Null i de to oprindelige versioner. Viser de værdier der FAKTISK blev brugt til
    /// at bygge SQL-strengen.
    /// </summary>
    public FilterInfo? Filter { get; set; }
}

/// <summary>Beskrivelse af den anvendte inputfiltrering for ét request.</summary>
public class FilterInfo
{
    public string Rule { get; set; } = "";
    public List<FilterField> Fields { get; set; } = [];
}

/// <summary>Ét felt før/efter filtrering.</summary>
public class FilterField
{
    public string Name { get; set; } = "";
    public string Original { get; set; } = "";
    public string Filtered { get; set; } = "";
    public bool Changed { get; set; }
}
