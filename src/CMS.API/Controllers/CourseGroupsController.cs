using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/course-groups")]
public class CourseGroupsController : ControllerBase
{
    private readonly ICourseGroupRepository _repository;

    public CourseGroupsController(ICourseGroupRepository repository)
    {
        _repository = repository;
    }

    /// <summary>All course groups.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CourseGroup>>> GetAll()
    {
        var courseGroups = await _repository.GetAllAsync();
        return Ok(courseGroups);
    }

    /// <summary>Filtered course group search.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IEnumerable<CourseGroup>>> Query([FromBody] CourseGroupQuery query)
    {
        var courseGroups = await _repository.QueryAsync(query);
        return Ok(courseGroups);
    }

    /// <summary>Single course group.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CourseGroup>> GetById(short id)
    {
        var courseGroup = await _repository.GetByIdAsync(id);
        return courseGroup is null ? NotFound() : Ok(courseGroup);
    }

    /// <summary>Create a course group (pkid assigned by IDENTITY).</summary>
    [HttpPost]
    public async Task<ActionResult<CourseGroup>> Create([FromBody] CourseGroupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
            return BadRequest(new { message = "群組名稱為必填。" });

        var pkid = await _repository.CreateAsync(request);
        var created = await _repository.GetByIdAsync(pkid);
        return CreatedAtAction(nameof(GetById), new { id = pkid }, created);
    }

    /// <summary>Update a course group (pkid in body).</summary>
    [HttpPut]
    public async Task<ActionResult<CourseGroup>> Update([FromBody] CourseGroupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
            return BadRequest(new { message = "群組名稱為必填。" });

        var updated = await _repository.UpdateAsync(request);
        if (!updated)
            return NotFound();

        var courseGroup = await _repository.GetByIdAsync(request.Pkid);
        return Ok(courseGroup);
    }

    /// <summary>Delete a course group.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(short id)
    {
        var deleted = await _repository.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
