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

    /// <summary>All publish statuses (發布狀態).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PublishStatus>>> GetAll(CancellationToken ct)
        => Ok(await _repository.GetAllAsync(ct));

    /// <summary>Filtered search.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IReadOnlyList<PublishStatus>>> Query([FromBody] PublishStatusQuery query, CancellationToken ct)
        => Ok(await _repository.QueryAsync(query, ct));

    /// <summary>Single status by pkid.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PublishStatus>> GetById(byte id, CancellationToken ct)
    {
        var status = await _repository.GetByIdAsync(id, ct);
        return status is null ? NotFound() : Ok(status);
    }

    /// <summary>Create a status. 409 if pkid already exists.</summary>
    [HttpPost]
    public async Task<ActionResult<PublishStatus>> Create([FromBody] PublishStatusRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        if (await _repository.ExistsAsync(request.Pkid, ct))
            return Conflict(new { message = $"主代碼「{request.Pkid}」已存在。" });

        var created = await _repository.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Pkid }, created);
    }

    /// <summary>Update a status. pkid (in body) identifies the record. 404 if not found.</summary>
    [HttpPut]
    public async Task<ActionResult> Update([FromBody] PublishStatusRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var updated = await _repository.UpdateAsync(request, ct);
        return updated ? NoContent() : NotFound();
    }

    /// <summary>Delete a status by pkid. 404 if not found.</summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(byte id, CancellationToken ct)
    {
        var deleted = await _repository.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
