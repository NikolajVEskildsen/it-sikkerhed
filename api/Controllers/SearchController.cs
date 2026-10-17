using Microsoft.AspNetCore.Mvc;
using api.Models;
using api.Services;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly IQueryService _service;

    public SearchController(IQueryService service) => _service = service;

    // POST /api/SecureSearch
    [HttpPost("SecureSearch")]
    public QueryResult SecureSearch([FromBody] SearchRequest req)
        => _service.SecureSearch(req.Term);

    // POST /api/UnsecureSearch
    [HttpPost("UnsecureSearch")]
    public QueryResult UnsecureSearch([FromBody] SearchRequest req)
        => _service.UnsecureSearch(req.Term);
}
