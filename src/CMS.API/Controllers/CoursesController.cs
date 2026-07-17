using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController : ControllerBase
{
    private readonly ICourseRepository _repository;

    public CoursesController(ICourseRepository repository)
    {
        _repository = repository;
    }

    /// <summary>All courses (FK labels JOINed).</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Course>>> GetAll()
    {
        var courses = await _repository.GetAllAsync();
        return Ok(courses);
    }

    /// <summary>Filtered course search.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IEnumerable<Course>>> Query([FromBody] CourseQuery query)
    {
        var courses = await _repository.QueryAsync(query);
        return Ok(courses);
    }

    /// <summary>Single course (includes N-N Certification / JobCategory pkid lists).</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Course>> GetById(int id)
    {
        var course = await _repository.GetByIdAsync(id);
        return course is null ? NotFound() : Ok(course);
    }

    /// <summary>Create a course (pkid assigned by IDENTITY).</summary>
    [HttpPost]
    public async Task<ActionResult<Course>> Create([FromBody] CourseRequest request)
    {
        var error = Validate(request);
        if (error is not null)
            return BadRequest(new { message = error });

        var pkid = await _repository.CreateAsync(request);
        var created = await _repository.GetByIdAsync(pkid);
        return CreatedAtAction(nameof(GetById), new { id = pkid }, created);
    }

    /// <summary>Update a course (pkid in body).</summary>
    [HttpPut]
    public async Task<ActionResult<Course>> Update([FromBody] CourseRequest request)
    {
        var error = Validate(request);
        if (error is not null)
            return BadRequest(new { message = error });

        var updated = await _repository.UpdateAsync(request);
        if (!updated)
            return NotFound();

        var course = await _repository.GetByIdAsync(request.Pkid);
        return Ok(course);
    }

    /// <summary>Delete a course (also clears both junction tables).</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _repository.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Required-string guards; returns the first error message, or null when valid.</summary>
    private static string? Validate(CourseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return "課程名稱為必填。";
        if (string.IsNullOrWhiteSpace(request.CourseId))
            return "簡介代碼為必填。";
        if (string.IsNullOrWhiteSpace(request.ProdCourseId))
            return "科目代碼為必填。";
        if (string.IsNullOrWhiteSpace(request.FriendlyUrl))
            return "友善網址為必填。";
        return null;
    }
}
