using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/publish-statuses")]
public class PublishStatusesController : ControllerBase
{
    private readonly IPublishStatusRepository _repository;

    public PublishStatusesController(IPublishStatusRepository repository)
    {
        _repository = repository;
    }

    /// <summary>All publish statuses.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PublishStatus>>> GetAll()
    {
        var statuses = await _repository.GetAllAsync();
        return Ok(statuses);
    }

    /// <summary>Filtered publish-status search.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IEnumerable<PublishStatus>>> Query([FromBody] PublishStatusQuery query)
    {
        var statuses = await _repository.QueryAsync(query);
        return Ok(statuses);
    }

    /// <summary>Single publish status.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PublishStatus>> GetById(byte id)
    {
        var status = await _repository.GetByIdAsync(id);
        return status is null ? NotFound() : Ok(status);
    }

    /// <summary>Create a publish status (pkid supplied by caller).</summary>
    [HttpPost]
    public async Task<ActionResult<PublishStatus>> Create([FromBody] PublishStatusRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
            return BadRequest(new { message = "狀態說明為必填。" });

        if (await _repository.ExistsAsync(request.Pkid))
            return Conflict(new { message = $"主代碼「{request.Pkid}」已存在。" });

        var pkid = await _repository.CreateAsync(request);
        var created = await _repository.GetByIdAsync(pkid);
        return CreatedAtAction(nameof(GetById), new { id = pkid }, created);
    }

    /// <summary>Update a publish status (pkid in body).</summary>
    [HttpPut]
    public async Task<ActionResult<PublishStatus>> Update([FromBody] PublishStatusRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
            return BadRequest(new { message = "狀態說明為必填。" });

        var updated = await _repository.UpdateAsync(request);
        if (!updated)
            return NotFound();

        var status = await _repository.GetByIdAsync(request.Pkid);
        return Ok(status);
    }

    /// <summary>Delete a publish status.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(byte id)
    {
        var deleted = await _repository.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
