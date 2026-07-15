using CMS.API.Models;

namespace CMS.API.Repositories;

public interface IAppUserRepository
{
    Task<IReadOnlyList<AppUser>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AppUser>> QueryAsync(AppUserQuery query, CancellationToken ct = default);
    Task<AppUser?> GetByIdAsync(string userId, CancellationToken ct = default);
    Task<bool> ExistsAsync(string userId, CancellationToken ct = default);

    /// <summary>Inserts the user (hashing the SysConfig default password into PasswordHash) and its
    /// AppUserRole rows. Returns the created record.</summary>
    Task<AppUser> CreateAsync(AppUserRequest request, CancellationToken ct = default);

    /// <summary>Updates the user (UserId immutable) and re-syncs AppUserRole rows. Does NOT touch
    /// PasswordHash / PasswordUpdatedTime. Returns false if the user does not exist.</summary>
    Task<bool> UpdateAsync(AppUserRequest request, CancellationToken ct = default);

    Task<bool> DeleteAsync(string userId, CancellationToken ct = default);

    /// <summary>Resets PasswordHash to the SysConfig default password and bumps
    /// PasswordUpdatedTime. Returns false if the user does not exist.</summary>
    Task<bool> ResetPasswordAsync(string userId, CancellationToken ct = default);
}
