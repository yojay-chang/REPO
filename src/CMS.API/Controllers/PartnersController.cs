using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/partners")]
public class PartnersController : ControllerBase
{
    private readonly IPartnerRepository _repository;

    public PartnersController(IPartnerRepository repository)
    {
        _repository = repository;
    }

    /// <summary>All partners.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Partner>>> GetAll()
    {
        var partners = await _repository.GetAllAsync();
        return Ok(partners);
    }

    /// <summary>Filtered partner search.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IEnumerable<Partner>>> Query([FromBody] PartnerQuery query)
    {
        var partners = await _repository.QueryAsync(query);
        return Ok(partners);
    }

    /// <summary>Single partner.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Partner>> GetById(short id)
    {
        var partner = await _repository.GetByIdAsync(id);
        return partner is null ? NotFound() : Ok(partner);
    }

    /// <summary>Create a partner (pkid assigned by IDENTITY).</summary>
    [HttpPost]
    public async Task<ActionResult<Partner>> Create([FromBody] PartnerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "廠商名稱為必填。" });
        if (string.IsNullOrWhiteSpace(request.AppKey))
            return BadRequest(new { message = "應用金鑰為必填。" });
        if (string.IsNullOrWhiteSpace(request.NameOnPartnerMenu))
            return BadRequest(new { message = "廠商選單顯示名稱為必填。" });
        if (string.IsNullOrWhiteSpace(request.NameOnCourseDetailPage))
            return BadRequest(new { message = "課程詳細頁顯示名稱為必填。" });

        var pkid = await _repository.CreateAsync(request);
        var created = await _repository.GetByIdAsync(pkid);
        return CreatedAtAction(nameof(GetById), new { id = pkid }, created);
    }

    /// <summary>Update a partner (pkid in body).</summary>
    [HttpPut]
    public async Task<ActionResult<Partner>> Update([FromBody] PartnerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "廠商名稱為必填。" });
        if (string.IsNullOrWhiteSpace(request.AppKey))
            return BadRequest(new { message = "應用金鑰為必填。" });
        if (string.IsNullOrWhiteSpace(request.NameOnPartnerMenu))
            return BadRequest(new { message = "廠商選單顯示名稱為必填。" });
        if (string.IsNullOrWhiteSpace(request.NameOnCourseDetailPage))
            return BadRequest(new { message = "課程詳細頁顯示名稱為必填。" });

        var updated = await _repository.UpdateAsync(request);
        if (!updated)
            return NotFound();

        var partner = await _repository.GetByIdAsync(request.Pkid);
        return Ok(partner);
    }

    /// <summary>Delete a partner.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(short id)
    {
        var deleted = await _repository.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
