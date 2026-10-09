using Microsoft.AspNetCore.Mvc;
using api.Models;
using api.Services;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QueryController : ControllerBase
{
    private readonly IQueryService _service;

    public QueryController(IQueryService service) => _service = service;

    // POST /api/query — udfører den SQL frontend sender.
    [HttpPost]
    public QueryResult Execute([FromBody] QueryRequest request) => _service.Execute(request);

    // GET /api/query/health
    [HttpGet("health")]
    public string Health() => "ok";
}
