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

    // POST /api/login
    [HttpPost]
    public QueryResult Login([FromBody] LoginRequest req)
        => _service.Login(req.Username, req.Password, req.Secure);
}
