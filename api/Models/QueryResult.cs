namespace api.Models;

/// <summary>Resultatet af en forespørgsel: kolonner + rækker, eller en fejl.</summary>
public class QueryResult
{
    public string[] Columns { get; set; } = [];
    public List<string[]> Rows { get; set; } = [];
    public string? Error { get; set; }
}
