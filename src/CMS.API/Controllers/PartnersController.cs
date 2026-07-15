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

    /// <summary>All partners (合作廠商).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Partner>>> GetAll(CancellationToken ct)
        => Ok(await _repository.GetAllAsync(ct));

    /// <summary>Filtered search.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IReadOnlyList<Partner>>> Query([FromBody] PartnerQuery query, CancellationToken ct)
        => Ok(await _repository.QueryAsync(query, ct));

    /// <summary>Single partner by pkid.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Partner>> GetById(short id, CancellationToken ct)
    {
        var partner = await _repository.GetByIdAsync(id, ct);
        return partner is null ? NotFound() : Ok(partner);
    }

    /// <summary>Create a partner. pkid is auto-generated (IDENTITY).</summary>
    [HttpPost]
    public async Task<ActionResult<Partner>> Create([FromBody] PartnerRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var created = await _repository.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Pkid }, created);
    }

    /// <summary>Update a partner. pkid (in body) identifies the record. 404 if not found.</summary>
    [HttpPut]
    public async Task<ActionResult> Update([FromBody] PartnerRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var updated = await _repository.UpdateAsync(request, ct);
        return updated ? NoContent() : NotFound();
    }

    /// <summary>Delete a partner by pkid. 404 if not found.</summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(short id, CancellationToken ct)
    {
        var deleted = await _repository.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
