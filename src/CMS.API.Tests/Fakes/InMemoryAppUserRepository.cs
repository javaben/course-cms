using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IAppUserRepository"/> for controller tests — mirrors the SQL behaviour
/// (keyword LIKE, tri-state IsActive match, IsActive/UserId ordering, n-n role sync, reset-password
/// bumping PasswordUpdatedTime) without a database. Password hashing is a no-op here — the fake has
/// no SysConfig, and PasswordHash is never exposed on the model anyway.
/// </summary>
public sealed class InMemoryAppUserRepository : IAppUserRepository
{
    private readonly List<AppUser> _users = [];
    private int _nextPkid = 1;

    public InMemoryAppUserRepository Seed(params AppUser[] users)
    {
        foreach (var u in users)
        {
            if (u.Pkid == 0) u.Pkid = _nextPkid;
            _nextPkid = Math.Max(_nextPkid, u.Pkid) + 1;
            u.RoleCount = u.RoleIds.Count;
            _users.Add(u);
        }
        return this;
    }

    public Task<IReadOnlyList<AppUser>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult(Ordered(_users));

    public Task<IReadOnlyList<AppUser>> QueryAsync(AppUserQuery query, CancellationToken ct = default)
    {
        IEnumerable<AppUser> q = _users;

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            q = q.Where(u =>
                u.UserId.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                u.UserName.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        if (query.IsActive.HasValue)
            q = q.Where(u => u.IsActive == query.IsActive.Value);

        return Task.FromResult(Ordered(q));
    }

    public Task<AppUser?> GetByIdAsync(string userId, CancellationToken ct = default)
        => Task.FromResult(Find(userId));

    public Task<bool> ExistsAsync(string userId, CancellationToken ct = default)
        => Task.FromResult(Find(userId) is not null);

    public Task<AppUser> CreateAsync(AppUserRequest request, CancellationToken ct = default)
    {
        var user = new AppUser
        {
            Pkid = _nextPkid++,
            UserId = request.UserId,
            UserName = request.UserName,
            IsActive = request.IsActive,
            PasswordUpdatedTime = DateTime.UtcNow, // password set server-side on create
            RoleIds = Distinct(request.RoleIds),
        };
        user.RoleCount = user.RoleIds.Count;
        _users.Add(user);
        return Task.FromResult(user);
    }

    public Task<bool> UpdateAsync(AppUserRequest request, CancellationToken ct = default)
    {
        var user = Find(request.UserId);
        if (user is null) return Task.FromResult(false);

        // PasswordUpdatedTime deliberately untouched on update.
        user.UserName = request.UserName;
        user.IsActive = request.IsActive;
        user.RoleIds = Distinct(request.RoleIds);
        user.RoleCount = user.RoleIds.Count;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(string userId, CancellationToken ct = default)
    {
        var user = Find(userId);
        if (user is null) return Task.FromResult(false);
        _users.Remove(user);
        return Task.FromResult(true);
    }

    public Task<bool> ResetPasswordAsync(string userId, CancellationToken ct = default)
    {
        var user = Find(userId);
        if (user is null) return Task.FromResult(false);
        user.PasswordUpdatedTime = DateTime.UtcNow;
        return Task.FromResult(true);
    }

    private AppUser? Find(string userId)
        => _users.FirstOrDefault(u => string.Equals(u.UserId, userId, StringComparison.OrdinalIgnoreCase));

    private static List<string> Distinct(IEnumerable<string> ids)
        => ids.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToList();

    private static IReadOnlyList<AppUser> Ordered(IEnumerable<AppUser> src)
        => src.OrderByDescending(u => u.IsActive).ThenBy(u => u.UserId, StringComparer.OrdinalIgnoreCase).ToList();
}
