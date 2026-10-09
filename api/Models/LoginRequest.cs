namespace api.Models;

public class LoginRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";

    /// <summary>true = kør den sikre (parameteriserede) vej; false = den sårbare.</summary>
    public bool Secure { get; set; }
}
