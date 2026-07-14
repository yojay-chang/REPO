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

    /// <summary>Slim AppUser list for the role-users multiselect.</summary>
    [HttpGet("app-users")]
    public async Task<ActionResult<IEnumerable<AppUserLookup>>> GetAppUsers()
    {
        var users = await _repository.GetAppUsersAsync();
        return Ok(users);
    }
}
