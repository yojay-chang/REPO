using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/rowaudit")]
public class RowAuditController : ControllerBase
{
    private readonly IRowAuditRepository _repository;

    public RowAuditController(IRowAuditRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// The audit trail for one record, newest first —
    /// e.g. <c>GET /api/rowaudit?tableName=Course&amp;pkid=123</c>.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RowAuditEntry>>> GetForRecord(
        [FromQuery] string? tableName,
        [FromQuery] int pkid)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return BadRequest(new { message = "tableName 為必填。" });

        var entries = await _repository.GetForRecordAsync(tableName, pkid);
        return Ok(entries);
    }
}
