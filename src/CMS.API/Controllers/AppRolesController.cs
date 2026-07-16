using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

// Admin-only: roles carry PermissionLevel, so create/update/delete here is a privilege boundary.
// The global fallback policy only requires *authentication*; without this any authenticated user
// could edit roles. Mirrors the Admin gate on AuthController.reset-password.
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/app-roles")]
public class AppRolesController : ControllerBase
{
    private readonly IAppRoleRepository _repository;

    public AppRolesController(IAppRoleRepository repository)
    {
        _repository = repository;
    }

    /// <summary>All roles (角色).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppRole>>> GetAll(CancellationToken ct)
        => Ok(await _repository.GetAllAsync(ct));

    /// <summary>Filtered search.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IReadOnlyList<AppRole>>> Query([FromBody] AppRoleQuery query, CancellationToken ct)
        => Ok(await _repository.QueryAsync(query, ct));

    /// <summary>Single role by RoleId (string PK).</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AppRole>> GetById(string id, CancellationToken ct)
    {
        var role = await _repository.GetByIdAsync(id, ct);
        return role is null ? NotFound() : Ok(role);
    }

    /// <summary>Create a role. 409 if RoleId already exists.</summary>
    [HttpPost]
    public async Task<ActionResult<AppRole>> Create([FromBody] AppRoleRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        if (await _repository.ExistsAsync(request.RoleId, ct))
            return Conflict(new { message = $"角色代碼「{request.RoleId}」已存在。" });

        var created = await _repository.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.RoleId }, created);
    }

    /// <summary>Update a role. RoleId (in body) identifies the record. 404 if not found.</summary>
    [HttpPut]
    public async Task<ActionResult> Update([FromBody] AppRoleRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var updated = await _repository.UpdateAsync(request, ct);
        return updated ? NoContent() : NotFound();
    }

    /// <summary>Delete a role by RoleId. 404 if not found.</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id, CancellationToken ct)
    {
        var deleted = await _repository.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
