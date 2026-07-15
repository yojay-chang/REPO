using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/featured-promo-items")]
public class FeaturedPromoItemsController : ControllerBase
{
    private readonly IFeaturedPromoItemRepository _repository;

    public FeaturedPromoItemsController(IFeaturedPromoItemRepository repository)
    {
        _repository = repository;
    }

    /// <summary>All featured promo items (PromoCode / TrainingCenter labels JOINed).</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FeaturedPromoItem>>> GetAll()
    {
        var items = await _repository.GetAllAsync();
        return Ok(items);
    }

    /// <summary>Weekly scheduler search — filter by TrainingCenter and a Monday–Sunday ScheduleOn range.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IEnumerable<FeaturedPromoItem>>> Query([FromBody] FeaturedPromoItemQuery query)
    {
        var items = await _repository.QueryAsync(query);
        return Ok(items);
    }

    /// <summary>Single featured promo item (PromoCode / TrainingCenter labels JOINed).</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<FeaturedPromoItem>> GetById(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>Create a featured promo item (pkid assigned by IDENTITY).</summary>
    [HttpPost]
    public async Task<ActionResult<FeaturedPromoItem>> Create([FromBody] FeaturedPromoItemRequest request)
    {
        var error = Validate(request);
        if (error is not null)
            return BadRequest(new { message = error });

        var pkid = await _repository.CreateAsync(request);
        var created = await _repository.GetByIdAsync(pkid);
        return CreatedAtAction(nameof(GetById), new { id = pkid }, created);
    }

    /// <summary>Update a featured promo item (pkid in body).</summary>
    [HttpPut]
    public async Task<ActionResult<FeaturedPromoItem>> Update([FromBody] FeaturedPromoItemRequest request)
    {
        var error = Validate(request);
        if (error is not null)
            return BadRequest(new { message = error });

        var updated = await _repository.UpdateAsync(request);
        if (!updated)
            return NotFound();

        var item = await _repository.GetByIdAsync(request.Pkid);
        return Ok(item);
    }

    /// <summary>Delete a featured promo item.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _repository.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Required-field guards; returns the first error message, or null when valid.</summary>
    private static string? Validate(FeaturedPromoItemRequest request)
    {
        if (request.PromotionPkid <= 0)
            return "促銷活動為必填。";
        if (request.TrainingCenterPkid <= 0)
            return "訓練中心為必填。";
        if (request.Slot is < 1 or > 3)
            return "版位僅能為 1、2 或 3。";
        if (string.IsNullOrWhiteSpace(request.Topic))
            return "主題為必填。";
        if (string.IsNullOrWhiteSpace(request.Description))
            return "說明為必填。";
        return null;
    }
}
