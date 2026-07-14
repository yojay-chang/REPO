using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/app-roles")]
public class AppRolesController : ControllerBase
{
    private readonly IAppRoleRepository _repository;

    public AppRolesController(IAppRoleRepository repository)
    {
        _repository = repository;
    }

    /// <summary>All roles.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppRole>>> GetAll()
    {
        var roles = await _repository.GetAllAsync();
        return Ok(roles);
    }

    /// <summary>Filtered role search.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IEnumerable<AppRole>>> Query([FromBody] AppRoleQuery query)
    {
        var roles = await _repository.QueryAsync(query);
        return Ok(roles);
    }

    /// <summary>Single role (includes assigned UserIds).</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AppRole>> GetById(string id)
    {
        var role = await _repository.GetByIdAsync(id);
        return role is null ? NotFound() : Ok(role);
    }

    /// <summary>Create a role.</summary>
    [HttpPost]
    public async Task<ActionResult<AppRole>> Create([FromBody] AppRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RoleId))
            return BadRequest(new { message = "RoleId 為必填。" });
        if (string.IsNullOrWhiteSpace(request.RoleName))
            return BadRequest(new { message = "RoleName 為必填。" });

        if (await _repository.ExistsAsync(request.RoleId))
            return Conflict(new { message = $"角色代碼「{request.RoleId}」已存在。" });

        var roleId = await _repository.CreateAsync(request);
        var created = await _repository.GetByIdAsync(roleId);
        return CreatedAtAction(nameof(GetById), new { id = roleId }, created);
    }

    /// <summary>Update a role (RoleId in body).</summary>
    [HttpPut]
    public async Task<ActionResult<AppRole>> Update([FromBody] AppRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RoleId))
            return BadRequest(new { message = "RoleId 為必填。" });
        if (string.IsNullOrWhiteSpace(request.RoleName))
            return BadRequest(new { message = "RoleName 為必填。" });

        var updated = await _repository.UpdateAsync(request);
        if (!updated)
            return NotFound();

        var role = await _repository.GetByIdAsync(request.RoleId);
        return Ok(role);
    }

    /// <summary>Delete a role.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _repository.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
