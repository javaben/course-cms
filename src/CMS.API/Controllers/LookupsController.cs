using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/lookups")]
public class LookupsController : ControllerBase
{
    private readonly ILookupRepository _repository;

    public LookupsController(ILookupRepository repository)
    {
        _repository = repository;
    }

    /// <summary>AppUser lookup list for the AppRole n-n picker.</summary>
    [HttpGet("app-users")]
    public async Task<ActionResult<IReadOnlyList<AppUserLookup>>> GetAppUsers(CancellationToken ct)
        => Ok(await _repository.GetAppUsersAsync(ct));
}
