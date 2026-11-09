namespace api.Models;

public class SearchRequest
{
    public string Term { get; set; } = "";

    /// <summary>
    /// Legacy-felt. Versionen vælges nu af ENDPOINTET (UnsecureSearch / FilteredSearch /
    /// SecureSearch), ikke af dette flag. Bevaret for bagudkompatibilitet; ignoreres.
    /// </summary>
    public bool Secure { get; set; }
}
