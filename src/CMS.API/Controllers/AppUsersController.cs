using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

// Admin-only: user management (create/update roles via RoleIds, delete, password reset) is a
// privilege-escalation surface. The global fallback policy only requires *authentication*, so
// without this a non-admin could grant themselves the Admin role or reset an admin's password.
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/app-users")]
public class AppUsersController : ControllerBase
{
    private readonly IAppUserRepository _repository;

    public AppUsersController(IAppUserRepository repository)
    {
        _repository = repository;
    }

    /// <summary>All users (使用者).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppUser>>> GetAll(CancellationToken ct)
        => Ok(await _repository.GetAllAsync(ct));

    /// <summary>Filtered search.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IReadOnlyList<AppUser>>> Query([FromBody] AppUserQuery query, CancellationToken ct)
        => Ok(await _repository.QueryAsync(query, ct));

    /// <summary>Single user by UserId (string PK).</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AppUser>> GetById(string id, CancellationToken ct)
    {
        var user = await _repository.GetByIdAsync(id, ct);
        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>Create a user. 409 if UserId already exists. Password is set server-side.</summary>
    [HttpPost]
    public async Task<ActionResult<AppUser>> Create([FromBody] AppUserRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        if (await _repository.ExistsAsync(request.UserId, ct))
            return Conflict(new { message = $"使用者代碼「{request.UserId}」已存在。" });

        var created = await _repository.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.UserId }, created);
    }

    /// <summary>Update a user. UserId (in body) identifies the record; PasswordHash is not touched.
    /// 404 if not found.</summary>
    [HttpPut]
    public async Task<ActionResult> Update([FromBody] AppUserRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var updated = await _repository.UpdateAsync(request, ct);
        return updated ? NoContent() : NotFound();
    }

    /// <summary>Delete a user by UserId. 404 if not found.</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id, CancellationToken ct)
    {
        var deleted = await _repository.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Reset the user's password to the SysConfig default. 404 if not found.</summary>
    [HttpPost("{id}/reset-password")]
    public async Task<ActionResult> ResetPassword(string id, CancellationToken ct)
    {
        var reset = await _repository.ResetPasswordAsync(id, ct);
        return reset ? NoContent() : NotFound();
    }
}
