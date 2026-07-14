using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IAppRoleRepository"/> for controller tests — mirrors the
/// SQL behaviour (keyword LIKE, permission-level match, n-n user sync) without a database.
/// </summary>
public sealed class InMemoryAppRoleRepository : IAppRoleRepository
{
    private readonly List<AppRole> _roles = [];
    private int _nextPkid = 1;

    public InMemoryAppRoleRepository Seed(params AppRole[] roles)
    {
        foreach (var r in roles)
        {
            if (r.Pkid == 0) r.Pkid = _nextPkid;
            _nextPkid = Math.Max(_nextPkid, r.Pkid) + 1;
            r.UserCount = r.UserIds.Count;
            _roles.Add(r);
        }
        return this;
    }

    public Task<IReadOnlyList<AppRole>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<AppRole>>(Ordered(_roles));

    public Task<IReadOnlyList<AppRole>> QueryAsync(AppRoleQuery query, CancellationToken ct = default)
    {
        IEnumerable<AppRole> q = _roles;

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            q = q.Where(r =>
                r.RoleId.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                r.RoleName.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                (r.Description ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        if (query.PermissionLevel.HasValue)
            q = q.Where(r => r.PermissionLevel == query.PermissionLevel.Value);

        return Task.FromResult<IReadOnlyList<AppRole>>(Ordered(q));
    }

    public Task<AppRole?> GetByIdAsync(string roleId, CancellationToken ct = default)
        => Task.FromResult(Find(roleId));

    public Task<bool> ExistsAsync(string roleId, CancellationToken ct = default)
        => Task.FromResult(Find(roleId) is not null);

    public Task<AppRole> CreateAsync(AppRoleRequest request, CancellationToken ct = default)
    {
        var role = new AppRole
        {
            Pkid = _nextPkid++,
            RoleId = request.RoleId,
            RoleName = request.RoleName,
            PermissionLevel = request.PermissionLevel,
            Description = request.Description,
            UserIds = Distinct(request.UserIds),
        };
        role.UserCount = role.UserIds.Count;
        _roles.Add(role);
        return Task.FromResult(role);
    }

    public Task<bool> UpdateAsync(AppRoleRequest request, CancellationToken ct = default)
    {
        var role = Find(request.RoleId);
        if (role is null) return Task.FromResult(false);

        role.RoleName = request.RoleName;
        role.PermissionLevel = request.PermissionLevel;
        role.Description = request.Description;
        role.UserIds = Distinct(request.UserIds);
        role.UserCount = role.UserIds.Count;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(string roleId, CancellationToken ct = default)
    {
        var role = Find(roleId);
        if (role is null) return Task.FromResult(false);
        _roles.Remove(role);
        return Task.FromResult(true);
    }

    private AppRole? Find(string roleId)
        => _roles.FirstOrDefault(r => string.Equals(r.RoleId, roleId, StringComparison.OrdinalIgnoreCase));

    private static List<string> Distinct(IEnumerable<string> ids)
        => ids.Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().ToList();

    private static IReadOnlyList<AppRole> Ordered(IEnumerable<AppRole> src)
        => src.OrderBy(r => r.PermissionLevel).ThenBy(r => r.RoleId, StringComparer.OrdinalIgnoreCase).ToList();
}
