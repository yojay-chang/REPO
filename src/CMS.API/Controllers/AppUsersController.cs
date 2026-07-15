using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/app-users")]
public class AppUsersController : ControllerBase
{
    private readonly IAppUserRepository _repository;

    public AppUsersController(IAppUserRepository repository)
    {
        _repository = repository;
    }

    /// <summary>All users.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppUser>>> GetAll()
    {
        var users = await _repository.GetAllAsync();
        return Ok(users);
    }

    /// <summary>Filtered user search.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IEnumerable<AppUser>>> Query([FromBody] AppUserQuery query)
    {
        var users = await _repository.QueryAsync(query);
        return Ok(users);
    }

    /// <summary>Single user (includes assigned RoleIds).</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AppUser>> GetById(string id)
    {
        var user = await _repository.GetByIdAsync(id);
        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>Create a user. Password is set from the SysConfig default (hashed).</summary>
    [HttpPost]
    public async Task<ActionResult<AppUser>> Create([FromBody] AppUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
            return BadRequest(new { message = "UserId 為必填。" });
        if (string.IsNullOrWhiteSpace(request.UserName))
            return BadRequest(new { message = "UserName 為必填。" });

        if (await _repository.ExistsAsync(request.UserId))
            return Conflict(new { message = $"使用者代碼「{request.UserId}」已存在。" });

        var userId = await _repository.CreateAsync(request);
        var created = await _repository.GetByIdAsync(userId);
        return CreatedAtAction(nameof(GetById), new { id = userId }, created);
    }

    /// <summary>Update a user (UserId in body). Never modifies the password.</summary>
    [HttpPut]
    public async Task<ActionResult<AppUser>> Update([FromBody] AppUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
            return BadRequest(new { message = "UserId 為必填。" });
        if (string.IsNullOrWhiteSpace(request.UserName))
            return BadRequest(new { message = "UserName 為必填。" });

        var updated = await _repository.UpdateAsync(request);
        if (!updated)
            return NotFound();

        var user = await _repository.GetByIdAsync(request.UserId);
        return Ok(user);
    }

    /// <summary>Delete a user.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _repository.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
