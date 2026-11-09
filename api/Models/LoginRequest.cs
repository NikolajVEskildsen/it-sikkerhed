namespace api.Models;

public class LoginRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";

    /// <summary>
    /// Legacy-felt. Versionen vælges nu af ENDPOINTET (UnsecureLogin / FilteredLogin /
    /// SecureLogin), ikke af dette flag. Bevaret for bagudkompatibilitet; ignoreres.
    /// </summary>
    public bool Secure { get; set; }
}
