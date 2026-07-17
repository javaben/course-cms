using CMS.API.Infrastructure;
using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IAuthRepository"/> for AuthController tests. Seeded users carry a plaintext
/// password hashed exactly as the real repo stores it, so the controller's verification exercises the
/// genuine code path. The signing key is supplied separately via <see cref="FakeSigningKeyProvider"/>.
/// </summary>
public sealed class InMemoryAuthRepository : IAuthRepository
{
    // Stands in for SysConfig['appConfig'].defaultPassword (the real repo reads it from the DB).
    public const string DefaultPassword = "Default#123";

    private readonly List<AuthUser> _users = [];

    /// <summary>Tracks UpgradePasswordHashAsync calls so tests can assert on rehash-on-login.</summary>
    public List<string> UpgradedUserIds { get; } = [];

    /// <summary>Seeds a user, hashing <paramref name="password"/> exactly as the real repo would.</summary>
    public InMemoryAuthRepository Seed(string userId, string userName, string password, bool isActive, params string[] roleIds)
        => SeedWithHash(userId, userName, PasswordHasher.Hash(password), isActive, roleIds);

    /// <summary>
    /// Seeds a user with a pre-built <paramref name="passwordHash"/> — lets a test plant a legacy
    /// SHA-256 row and prove the login path still accepts and upgrades it.
    /// </summary>
    public InMemoryAuthRepository SeedWithHash(
        string userId, string userName, string passwordHash, bool isActive, params string[] roleIds)
    {
        _users.Add(new AuthUser
        {
            UserId = userId,
            UserName = userName,
            IsActive = isActive,
            PasswordHash = passwordHash,
            RoleIds = roleIds.ToList(),
        });
        return this;
    }

    public Task<AuthUser?> FindByUserIdAsync(string userId, CancellationToken ct = default)
        => Task.FromResult(_users.FirstOrDefault(
            u => string.Equals(u.UserId, userId, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> UpdateUserNameAsync(string userId, string userName, CancellationToken ct = default)
    {
        var user = Find(userId);
        if (user is null) return Task.FromResult(false);
        user.UserName = userName; // roles / IsActive / password deliberately untouched
        return Task.FromResult(true);
    }

    public Task<bool> ChangePasswordAsync(string userId, string newPasswordHash, CancellationToken ct = default)
    {
        var user = Find(userId);
        if (user is null) return Task.FromResult(false);
        user.PasswordHash = newPasswordHash;
        user.PasswordUpdatedTime = DateTime.UtcNow;
        return Task.FromResult(true);
    }

    public Task<bool> ResetPasswordToDefaultAsync(string userId, CancellationToken ct = default)
    {
        var user = Find(userId);
        if (user is null) return Task.FromResult(false);
        user.PasswordHash = PasswordHasher.Hash(DefaultPassword);
        user.PasswordUpdatedTime = DateTime.UtcNow;
        return Task.FromResult(true);
    }

    public Task<bool> UpgradePasswordHashAsync(string userId, string newPasswordHash, CancellationToken ct = default)
    {
        var user = Find(userId);
        if (user is null) return Task.FromResult(false);
        user.PasswordHash = newPasswordHash; // PasswordUpdatedTime deliberately untouched
        UpgradedUserIds.Add(userId);
        return Task.FromResult(true);
    }

    private AuthUser? Find(string userId)
        => _users.FirstOrDefault(u => string.Equals(u.UserId, userId, StringComparison.OrdinalIgnoreCase));
}
