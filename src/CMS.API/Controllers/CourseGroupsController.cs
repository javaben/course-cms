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

    /// <summary>All course groups (課程群組).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CourseGroup>>> GetAll(CancellationToken ct)
        => Ok(await _repository.GetAllAsync(ct));

    /// <summary>Filtered search.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IReadOnlyList<CourseGroup>>> Query([FromBody] CourseGroupQuery query, CancellationToken ct)
        => Ok(await _repository.QueryAsync(query, ct));

    /// <summary>Single group by pkid.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CourseGroup>> GetById(short id, CancellationToken ct)
    {
        var group = await _repository.GetByIdAsync(id, ct);
        return group is null ? NotFound() : Ok(group);
    }

    /// <summary>Create a group. pkid is auto-generated (IDENTITY).</summary>
    [HttpPost]
    public async Task<ActionResult<CourseGroup>> Create([FromBody] CourseGroupRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var created = await _repository.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Pkid }, created);
    }

    /// <summary>Update a group. pkid (in body) identifies the record. 404 if not found.</summary>
    [HttpPut]
    public async Task<ActionResult> Update([FromBody] CourseGroupRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var updated = await _repository.UpdateAsync(request, ct);
        return updated ? NoContent() : NotFound();
    }

    /// <summary>Delete a group by pkid. 404 if not found.</summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(short id, CancellationToken ct)
    {
        var deleted = await _repository.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
