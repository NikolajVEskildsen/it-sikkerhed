namespace api.Models;

public class SearchRequest
{
    public string Term { get; set; } = "";

    /// <summary>true = kør den sikre (parameteriserede) vej; false = den sårbare.</summary>
    public bool Secure { get; set; }
}
