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
    /// The audit history for one record, filtered by <paramref name="tableName"/> + <paramref name="pkid"/>,
    /// newest first. Example: <c>GET /api/rowaudit?tableName=Course&amp;pkid=123</c>.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RowAuditEntry>>> GetForRecord(
        [FromQuery] string? tableName, [FromQuery] string? pkid, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(pkid))
            return BadRequest(new { message = "tableName 與 pkid 為必填。" });

        return Ok(await _repository.GetForRecordAsync(tableName, pkid, ct));
    }

    /// <summary>The global audit log across all tables, newest first (capped).</summary>
    [HttpGet("all")]
    public async Task<ActionResult<IReadOnlyList<RowAuditListItem>>> GetAll(CancellationToken ct)
        => Ok(await _repository.QueryAsync(new RowAuditQuery(), ct));

    /// <summary>Filtered global audit log (TableName / ActionType / keyword), newest first (capped).</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IReadOnlyList<RowAuditListItem>>> Query([FromBody] RowAuditQuery query, CancellationToken ct)
        => Ok(await _repository.QueryAsync(query, ct));

    /// <summary>Distinct table names present in the audit log — for the filter picker.</summary>
    [HttpGet("tables")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetTables(CancellationToken ct)
        => Ok(await _repository.GetTableNamesAsync(ct));
}
