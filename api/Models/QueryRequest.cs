namespace api.Models;

/// <summary>
/// En forespørgsel fra frontend. Selve SQL'en ligger i frontend (som const) og
/// sendes med her. For den sikre version sendes input adskilt i <see cref="Parameters"/>.
/// </summary>
public class QueryRequest
{
    public string Sql { get; set; } = "";
    public Dictionary<string, string>? Parameters { get; set; }
}
