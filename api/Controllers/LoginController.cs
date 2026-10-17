using Microsoft.AspNetCore.Mvc;
using api.Models;
using api.Services;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoginController : ControllerBase
{
    private readonly IQueryService _service;

    public LoginController(IQueryService service) => _service = service;

    // POST /api/SecureLogin
    [HttpPost("SecureLogin")]
    public QueryResult SecureLogin([FromBody] LoginRequest req)
        => _service.SecureLogin(req.Username, req.Password);

    // POST /api/UnsecureLogin
    [HttpPost("UnsecureLogin")]
    public QueryResult UnsecureLogin([FromBody] LoginRequest req)
        => _service.UnsecureLogin(req.Username, req.Password);
}
