using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CMS.API.Infrastructure;
using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CMS.API.Controllers;

// No controller-level [AllowAnonymous]: login opts out per-action, while /profile requires a token
// via the global fallback policy. (A controller-level [AllowAnonymous] would leak onto /profile too.)
[ApiController]
[Route("api/[controller]")] // → api/Auth
public class AuthController : ControllerBase
{
    /// <summary>How long an issued access token stays valid.</summary>
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    /// <summary>Claim type carrying each of the user's RoleIds (also the JWT role-claim type).</summary>
    public const string RoleClaimType = "role";

    private const string InvalidCredentials = "invalid credentials";

    private readonly IAuthRepository _repository;
    private readonly ISigningKeyProvider _signingKeys;

    public AuthController(IAuthRepository repository, ISigningKeyProvider signingKeys)
    {
        _repository = repository;
        _signingKeys = signingKeys;
    }

    /// <summary>
    /// Verifies the credentials against AppUser (UserId match, IsActive = 1, password hash) and returns
    /// the user profile plus a signed 24h JWT. Any failed check returns a single generic 401 so the
    /// caller cannot tell which part was wrong. On success, a hash still stored in a legacy/weaker
    /// format is transparently upgraded — this is the only flow holding the plaintext.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var user = await _repository.FindByUserIdAsync(request.UserId, ct);
        var verification = PasswordHasher.Verify(user?.PasswordHash, request.Password);

        // Evaluate every check the same way and collapse to one generic result — never reveal which
        // failed (unknown user, inactive, or wrong password).
        var ok = user is not null
                 && user.IsActive
                 && verification != PasswordVerificationResult.Failed;
        if (!ok)
            return Unauthorized(new { message = InvalidCredentials });

        // The plaintext is only in hand here, so this is the one place a legacy/weaker hash can be
        // upgraded. Best-effort: the user is already authenticated, and failing to re-encode a hash
        // must not fail their login — the next login retries it.
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            await _repository.UpgradePasswordHashAsync(user!.UserId, PasswordHasher.Hash(request.Password), ct);

        var accessToken = BuildAccessToken(user!, _signingKeys.GetSecurityKey());

        return Ok(new LoginResponse
        {
            UserId = user!.UserId,
            UserName = user.UserName,
            AccessToken = accessToken,
        });
    }

    /// <summary>
    /// Updates the signed-in user's <c>UserName</c> only. The UserId is taken from the JWT (the
    /// authenticated user) — never from the request body — so a caller can only rename themselves,
    /// and roles are untouched. UserName is required and trimmed.
    /// </summary>
    [Authorize]
    [HttpPut("profile")]
    public async Task<ActionResult<ProfileResponse>> UpdateProfile(
        [FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var userName = request.UserName?.Trim();
        if (string.IsNullOrEmpty(userName))
        {
            ModelState.AddModelError(nameof(request.UserName), "使用者名稱為必填。");
            return ValidationProblem(ModelState);
        }

        // Identity comes from the token, not the body — the body's UserId (if any) is ignored.
        var userId = User.FindFirstValue("userId");
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var updated = await _repository.UpdateUserNameAsync(userId, userName, ct);
        if (!updated) return NotFound();

        return Ok(new ProfileResponse { UserId = userId, UserName = userName });
    }

    /// <summary>
    /// Changes the signed-in user's own password. Verifies the current password against the stored
    /// hash, enforces new-password complexity, requires new == confirm, then stores a fresh salted hash
    /// of the new password and bumps PasswordUpdatedTime. No password or hash is returned. The user is
    /// taken from the JWT.
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue("userId");
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var user = await _repository.FindByUserIdAsync(userId, ct);
        if (user is null) return Unauthorized();

        // 1. Current password must match the stored hash — otherwise change nothing.
        if (PasswordHasher.Verify(user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
            return BadRequest(new { message = "目前密碼不正確。" });

        // 2. New-password complexity.
        if (!PasswordPolicy.IsComplexEnough(request.NewPassword))
            return BadRequest(new { message = PasswordPolicy.ComplexityMessage });

        // 3. New and confirmation must match.
        if (!string.Equals(request.NewPassword, request.ConfirmNewPassword, StringComparison.Ordinal))
            return BadRequest(new { message = "新密碼與確認密碼不一致。" });

        // 4. Store the new hash + timestamp. (No rehash-on-change concern: Hash always writes the
        // current format, so a change-password inherently upgrades a legacy row too.)
        await _repository.ChangePasswordAsync(userId, PasswordHasher.Hash(request.NewPassword!), ct);
        return NoContent();
    }

    /// <summary>
    /// Resets a target user's password back to the SysConfig default. <b>Admin only</b> — enforced by
    /// the role claim, so a non-Admin caller gets 403. Reads the default at runtime, stores a freshly
    /// salted hash of it, bumps PasswordUpdatedTime. No password or hash is returned.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var reset = await _repository.ResetPasswordToDefaultAsync(request.UserId, ct);
        return reset ? NoContent() : NotFound();
    }

    /// <summary>
    /// Builds an HMAC-SHA256 signed JWT carrying the user id/name and one role claim per RoleId,
    /// expiring <see cref="TokenLifetime"/> after issue.
    /// </summary>
    private static string BuildAccessToken(AuthUser user, SymmetricSecurityKey signingKey)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId),
            new("userId", user.UserId),
            new("userName", user.UserName),
        };
        claims.AddRange(user.RoleIds
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => new Claim(RoleClaimType, r)));

        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        // Constructed directly (not via SecurityTokenDescriptor) so claim types are written verbatim,
        // with no inbound/outbound claim-type remapping.
        var token = new JwtSecurityToken(
            claims: claims,
            notBefore: now,
            expires: now.Add(TokenLifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
