using System.Data;
using System.Text.Json;
using CMS.API.Infrastructure;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public sealed class AppUserRepository : IAppUserRepository
{
    private const string TableName = "AppUser";

    private readonly IDbConnectionFactory _factory;
    private readonly IRowAuditWriter _audit;

    public AppUserRepository(IDbConnectionFactory factory, IRowAuditWriter audit)
    {
        _factory = factory;
        _audit = audit;
    }

    // PasswordHash is deliberately excluded from every SELECT — never returned to the client.
    private const string SelectColumns = @"
        SELECT u.pkid                AS Pkid,
               u.UserId              AS UserId,
               u.UserName            AS UserName,
               u.IsActive            AS IsActive,
               u.PasswordUpdatedTime AS PasswordUpdatedTime,
               (SELECT COUNT(*) FROM AppUserRole ur WHERE ur.UserId = u.UserId) AS RoleCount
        FROM AppUser u";

    public async Task<IReadOnlyList<AppUser>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var sql = $"{SelectColumns} ORDER BY u.IsActive DESC, u.UserId ASC";
        var rows = await conn.QueryAsync<AppUser>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<AppUser>> QueryAsync(AppUserQuery query, CancellationToken ct = default)
    {
        var where = new List<string>();
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            where.Add("(u.UserId LIKE @Keyword OR u.UserName LIKE @Keyword)");
            p.Add("@Keyword", $"%{query.Keyword.Trim()}%");
        }

        if (query.IsActive.HasValue)
        {
            where.Add("u.IsActive = @IsActive");
            p.Add("@IsActive", query.IsActive.Value);
        }

        var whereClause = where.Count > 0 ? $" WHERE {string.Join(" AND ", where)}" : "";
        var sql = $"{SelectColumns}{whereClause} ORDER BY u.IsActive DESC, u.UserId ASC";

        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<AppUser>(new CommandDefinition(sql, p, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<AppUser?> GetByIdAsync(string userId, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        return await LoadAsync(conn, null, userId, ct);
    }

    /// <summary>Loads one user (+ its RoleIds) on the given connection/transaction, for audit snapshots.</summary>
    private static async Task<AppUser?> LoadAsync(
        IDbConnection conn, IDbTransaction? tx, string userId, CancellationToken ct)
    {
        var sql = $"{SelectColumns} WHERE u.UserId = @UserId";
        var user = await conn.QuerySingleOrDefaultAsync<AppUser>(
            new CommandDefinition(sql, new { UserId = userId }, tx, cancellationToken: ct));
        if (user is null) return null;

        var roleIds = await conn.QueryAsync<string>(new CommandDefinition(
            "SELECT RoleId FROM AppUserRole WHERE UserId = @UserId ORDER BY RoleId",
            new { UserId = userId }, tx, cancellationToken: ct));
        user.RoleIds = roleIds.AsList();
        return user;
    }

    public async Task<bool> ExistsAsync(string userId, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM AppUser WHERE UserId = @UserId",
            new { UserId = userId }, cancellationToken: ct));
        return count > 0;
    }

    public async Task<AppUser> CreateAsync(AppUserRequest request, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);

        var passwordHash = await GetDefaultPasswordHashAsync(conn, null, ct);

        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO AppUser (UserId, UserName, IsActive, PasswordHash, PasswordUpdatedTime)
              VALUES (@UserId, @UserName, @IsActive, @PasswordHash, @PasswordUpdatedTime);",
            new
            {
                request.UserId,
                request.UserName,
                request.IsActive,
                PasswordHash = passwordHash,
                PasswordUpdatedTime = DateTime.UtcNow,
            },
            tx, cancellationToken: ct));

        await SyncRolesAsync(conn, tx, request.UserId, request.RoleIds, ct);

        var created = (await LoadAsync(conn, tx, request.UserId, ct))!;
        await _audit.LogInsertAsync(conn, tx, TableName, created, ct);

        tx.Commit();
        return created;
    }

    public async Task<bool> UpdateAsync(AppUserRequest request, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var before = await LoadAsync(conn, tx, request.UserId, ct);
        if (before is null) { tx.Rollback(); return false; }

        // PasswordHash / PasswordUpdatedTime deliberately untouched — only reset-password changes them.
        await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE AppUser
                 SET UserName = @UserName,
                     IsActive = @IsActive
               WHERE UserId = @UserId;",
            new { request.UserId, request.UserName, request.IsActive }, tx, cancellationToken: ct));

        await SyncRolesAsync(conn, tx, request.UserId, request.RoleIds, ct);

        var after = (await LoadAsync(conn, tx, request.UserId, ct))!;
        await _audit.LogUpdateAsync(conn, tx, TableName, before, after, ct);

        tx.Commit();
        return true;
    }

    public async Task<bool> DeleteAsync(string userId, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var before = await LoadAsync(conn, tx, userId, ct);
        if (before is null) { tx.Rollback(); return false; }

        // Remove n-n rows first (FK), then the user.
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM AppUserRole WHERE UserId = @UserId",
            new { UserId = userId }, tx, cancellationToken: ct));

        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM AppUser WHERE UserId = @UserId",
            new { UserId = userId }, tx, cancellationToken: ct));

        await _audit.LogDeleteAsync(conn, tx, TableName, before, ct);

        tx.Commit();
        return true;
    }

    public async Task<bool> ResetPasswordAsync(string userId, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);

        var passwordHash = await GetDefaultPasswordHashAsync(conn, null, ct);

        var affected = await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE AppUser
                 SET PasswordHash = @PasswordHash,
                     PasswordUpdatedTime = @Now
               WHERE UserId = @UserId;",
            new { UserId = userId, PasswordHash = passwordHash, Now = DateTime.UtcNow },
            cancellationToken: ct));

        return affected > 0;
    }

    /// <summary>n-n sync: delete-then-reinsert AppUserRole rows for the user.</summary>
    private static async Task SyncRolesAsync(
        IDbConnection conn, IDbTransaction tx, string userId, List<string> roleIds, CancellationToken ct)
    {
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM AppUserRole WHERE UserId = @UserId",
            new { UserId = userId }, tx, cancellationToken: ct));

        var distinct = roleIds.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToList();
        if (distinct.Count == 0) return;

        await conn.ExecuteAsync(new CommandDefinition(
            "INSERT INTO AppUserRole (UserId, RoleId) VALUES (@UserId, @RoleId)",
            distinct.Select(r => new { UserId = userId, RoleId = r }), tx, cancellationToken: ct));
    }

    /// <summary>
    /// Reads SysConfig['appConfig'].defaultPassword and returns its SHA-256 hash as lowercase hex.
    /// Throws if the config row or the defaultPassword property is missing (misconfiguration → 500).
    /// </summary>
    private static async Task<string> GetDefaultPasswordHashAsync(
        IDbConnection conn, IDbTransaction? tx, CancellationToken ct)
    {
        var json = await conn.ExecuteScalarAsync<string?>(new CommandDefinition(
            "SELECT configValue FROM SysConfig WHERE configKey = 'appConfig'",
            transaction: tx, cancellationToken: ct));

        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("SysConfig['appConfig'] 未設定，無法取得預設密碼。");

        string? defaultPassword;
        try
        {
            using var doc = JsonDocument.Parse(json);
            defaultPassword = doc.RootElement.TryGetProperty("defaultPassword", out var prop)
                ? prop.GetString()
                : null;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("SysConfig['appConfig'] 不是有效的 JSON。", ex);
        }

        if (string.IsNullOrEmpty(defaultPassword))
            throw new InvalidOperationException("appConfig.defaultPassword 未設定。");

        return PasswordHasher.Sha256Hex(defaultPassword);
    }
}
