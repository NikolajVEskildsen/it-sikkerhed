namespace api.Models;

/// <summary>Resultatet af en forespørgsel: hvilken SQL der kørte + kolonner/rækker, eller en fejl.</summary>
public class QueryResult
{
    public string Sql { get; set; } = "";
    public string? Parameters { get; set; }    // bundne parametre (kun sikker vej), ellers null
    public string[] Columns { get; set; } = [];
    public List<string[]> Rows { get; set; } = [];
    public string? Error { get; set; }
}
