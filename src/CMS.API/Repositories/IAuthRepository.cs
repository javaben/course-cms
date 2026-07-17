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
    /// (read at runtime): sets <c>PasswordHash</c> to a fresh hash of that default and bumps
    /// <c>PasswordUpdatedTime</c>. Returns <c>false</c> if no such user exists.
    /// </summary>
    Task<bool> ResetPasswordToDefaultAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Rewrites <c>PasswordHash</c> in place after a successful login re-hashed a legacy/weaker hash
    /// (see <see cref="Infrastructure.PasswordVerificationResult.SuccessRehashNeeded"/>). The password
    /// itself is unchanged, so <c>PasswordUpdatedTime</c> is deliberately <b>not</b> bumped — this is a
    /// storage-format upgrade, not a password change, and the timestamp means the latter.
    /// </summary>
    Task<bool> UpgradePasswordHashAsync(string userId, string newPasswordHash, CancellationToken ct = default);
}
