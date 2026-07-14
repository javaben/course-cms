using CMS.API.Models;

namespace CMS.API.Repositories;

public interface IAppRoleRepository
{
    Task<IReadOnlyList<AppRole>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AppRole>> QueryAsync(AppRoleQuery query, CancellationToken ct = default);
    Task<AppRole?> GetByIdAsync(string roleId, CancellationToken ct = default);
    Task<bool> ExistsAsync(string roleId, CancellationToken ct = default);

    /// <summary>Inserts the role and its AppUserRole rows. Returns the created record.</summary>
    Task<AppRole> CreateAsync(AppRoleRequest request, CancellationToken ct = default);

    /// <summary>Updates the role (RoleId immutable) and re-syncs AppUserRole rows.
    /// Returns false if the role does not exist.</summary>
    Task<bool> UpdateAsync(AppRoleRequest request, CancellationToken ct = default);

    Task<bool> DeleteAsync(string roleId, CancellationToken ct = default);
}
