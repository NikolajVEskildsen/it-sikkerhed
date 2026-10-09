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

    // POST /api/search
    [HttpPost]
    public QueryResult Search([FromBody] SearchRequest req)
        => _service.Search(req.Term, req.Secure);
}
