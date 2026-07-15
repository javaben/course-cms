using CMS.API.Models;

namespace CMS.API.Repositories;

public interface IAuthRepository
{
    /// <summary>
    /// Loads the credential row (incl. PasswordHash and role ids) for <paramref name="userId"/>,
    /// or <c>null</c> if no such user exists. The caller — not this method — decides whether the
    /// account is active and whether the password matches, so failures stay indistinguishable.
    /// </summary>
    Task<AuthUser?> FindByUserIdAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Updates only the <c>UserName</c> for <paramref name="userId"/>. Returns <c>false</c> if no
    /// such user exists. Never touches roles, IsActive, or the password.
    /// </summary>
    Task<bool> UpdateUserNameAsync(string userId, string userName, CancellationToken ct = default);

    /// <summary>
    /// Sets <c>PasswordHash</c> to <paramref name="newPasswordHash"/> and bumps
    /// <c>PasswordUpdatedTime</c> to now. Returns <c>false</c> if no such user exists.
    /// </summary>
    Task<bool> ChangePasswordAsync(string userId, string newPasswordHash, CancellationToken ct = default);

    /// <summary>
    /// Resets the user's password to the <c>defaultPassword</c> from <c>SysConfig['appConfig']</c>
    /// (read at runtime): sets <c>PasswordHash = SHA256(default)</c> and bumps
    /// <c>PasswordUpdatedTime</c>. Returns <c>false</c> if no such user exists.
    /// </summary>
    Task<bool> ResetPasswordToDefaultAsync(string userId, CancellationToken ct = default);
}
