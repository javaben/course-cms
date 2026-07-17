using CMS.API.Infrastructure;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public sealed class AuthRepository : IAuthRepository
{
    private readonly IDbConnectionFactory _factory;

    public AuthRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<AuthUser?> FindByUserIdAsync(string userId, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);

        // PasswordHash is read here (unlike AppUserRepository) purely to verify the login; it is
        // never surfaced past AuthController.
        var user = await conn.QuerySingleOrDefaultAsync<AuthUser>(new CommandDefinition(
            @"SELECT UserId       AS UserId,
                     UserName     AS UserName,
                     IsActive     AS IsActive,
                     PasswordHash AS PasswordHash
              FROM AppUser
              WHERE UserId = @UserId",
            new { UserId = userId }, cancellationToken: ct));
        if (user is null) return null;

        var roleIds = await conn.QueryAsync<string>(new CommandDefinition(
            "SELECT RoleId FROM AppUserRole WHERE UserId = @UserId ORDER BY RoleId",
            new { UserId = userId }, cancellationToken: ct));
        user.RoleIds = roleIds.AsList();
        return user;
    }

    public async Task<bool> UpdateUserNameAsync(string userId, string userName, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);

        var affected = await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE AppUser SET UserName = @UserName WHERE UserId = @UserId",
            new { UserId = userId, UserName = userName }, cancellationToken: ct));

        return affected > 0;
    }

    public async Task<bool> ChangePasswordAsync(string userId, string newPasswordHash, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);

        var affected = await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE AppUser SET PasswordHash = @Hash, PasswordUpdatedTime = @Now WHERE UserId = @UserId",
            new { UserId = userId, Hash = newPasswordHash, Now = DateTime.UtcNow }, cancellationToken: ct));

        return affected > 0;
    }

    public async Task<bool> ResetPasswordToDefaultAsync(string userId, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);

        // Read the default password from SysConfig at runtime, hash it, and store it.
        var json = await conn.ExecuteScalarAsync<string?>(new CommandDefinition(
            "SELECT configValue FROM SysConfig WHERE configKey = 'appConfig'", cancellationToken: ct));
        var defaultPassword = AppConfig.GetRequiredProperty(json, "defaultPassword");
        var hash = PasswordHasher.Hash(defaultPassword);

        var affected = await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE AppUser SET PasswordHash = @Hash, PasswordUpdatedTime = @Now WHERE UserId = @UserId",
            new { UserId = userId, Hash = hash, Now = DateTime.UtcNow }, cancellationToken: ct));

        return affected > 0;
    }

    public async Task<bool> UpgradePasswordHashAsync(
        string userId, string newPasswordHash, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);

        // PasswordUpdatedTime deliberately untouched — the password did not change, only its encoding.
        var affected = await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE AppUser SET PasswordHash = @Hash WHERE UserId = @UserId",
            new { UserId = userId, Hash = newPasswordHash }, cancellationToken: ct));

        return affected > 0;
    }
}
