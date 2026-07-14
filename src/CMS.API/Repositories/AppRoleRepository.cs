using System.Data;
using CMS.API.Infrastructure;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public sealed class AppRoleRepository : IAppRoleRepository
{
    private readonly IDbConnectionFactory _factory;

    public AppRoleRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    private const string SelectColumns = @"
        SELECT r.pkid              AS Pkid,
               r.RoleId            AS RoleId,
               r.RoleName          AS RoleName,
               r.PermissionLevel   AS PermissionLevel,
               r.Description       AS Description,
               (SELECT COUNT(*) FROM AppUserRole ur WHERE ur.RoleId = r.RoleId) AS UserCount
        FROM AppRole r";

    public async Task<IReadOnlyList<AppRole>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var sql = $"{SelectColumns} ORDER BY r.PermissionLevel ASC, r.RoleId ASC";
        var rows = await conn.QueryAsync<AppRole>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<AppRole>> QueryAsync(AppRoleQuery query, CancellationToken ct = default)
    {
        var where = new List<string>();
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            where.Add("(r.RoleId LIKE @Keyword OR r.RoleName LIKE @Keyword OR r.Description LIKE @Keyword)");
            p.Add("@Keyword", $"%{query.Keyword.Trim()}%");
        }

        if (query.PermissionLevel.HasValue)
        {
            where.Add("r.PermissionLevel = @PermissionLevel");
            p.Add("@PermissionLevel", query.PermissionLevel.Value);
        }

        var whereClause = where.Count > 0 ? $" WHERE {string.Join(" AND ", where)}" : "";
        var sql = $"{SelectColumns}{whereClause} ORDER BY r.PermissionLevel ASC, r.RoleId ASC";

        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<AppRole>(new CommandDefinition(sql, p, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<AppRole?> GetByIdAsync(string roleId, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);

        var sql = $"{SelectColumns} WHERE r.RoleId = @RoleId";
        var role = await conn.QuerySingleOrDefaultAsync<AppRole>(
            new CommandDefinition(sql, new { RoleId = roleId }, cancellationToken: ct));
        if (role is null) return null;

        var userIds = await conn.QueryAsync<string>(new CommandDefinition(
            "SELECT UserId FROM AppUserRole WHERE RoleId = @RoleId ORDER BY UserId",
            new { RoleId = roleId }, cancellationToken: ct));
        role.UserIds = userIds.AsList();
        return role;
    }

    public async Task<bool> ExistsAsync(string roleId, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM AppRole WHERE RoleId = @RoleId",
            new { RoleId = roleId }, cancellationToken: ct));
        return count > 0;
    }

    public async Task<AppRole> CreateAsync(AppRoleRequest request, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO AppRole (RoleId, RoleName, PermissionLevel, Description)
              VALUES (@RoleId, @RoleName, @PermissionLevel, @Description);",
            request, tx, cancellationToken: ct));

        await SyncUsersAsync(conn, tx, request.RoleId, request.UserIds, ct);
        tx.Commit();

        return (await GetByIdAsync(request.RoleId, ct))!;
    }

    public async Task<bool> UpdateAsync(AppRoleRequest request, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var affected = await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE AppRole
                 SET RoleName        = @RoleName,
                     PermissionLevel = @PermissionLevel,
                     Description     = @Description
               WHERE RoleId = @RoleId;",
            request, tx, cancellationToken: ct));

        if (affected == 0)
        {
            tx.Rollback();
            return false;
        }

        await SyncUsersAsync(conn, tx, request.RoleId, request.UserIds, ct);
        tx.Commit();
        return true;
    }

    public async Task<bool> DeleteAsync(string roleId, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        // Remove n-n rows first (FK), then the role.
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM AppUserRole WHERE RoleId = @RoleId",
            new { RoleId = roleId }, tx, cancellationToken: ct));

        var affected = await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM AppRole WHERE RoleId = @RoleId",
            new { RoleId = roleId }, tx, cancellationToken: ct));

        tx.Commit();
        return affected > 0;
    }

    /// <summary>n-n sync: delete-then-reinsert AppUserRole rows for the role.</summary>
    private static async Task SyncUsersAsync(
        IDbConnection conn, IDbTransaction tx, string roleId, List<string> userIds, CancellationToken ct)
    {
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM AppUserRole WHERE RoleId = @RoleId",
            new { RoleId = roleId }, tx, cancellationToken: ct));

        var distinct = userIds.Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().ToList();
        if (distinct.Count == 0) return;

        await conn.ExecuteAsync(new CommandDefinition(
            "INSERT INTO AppUserRole (UserId, RoleId) VALUES (@UserId, @RoleId)",
            distinct.Select(u => new { UserId = u, RoleId = roleId }), tx, cancellationToken: ct));
    }
}
