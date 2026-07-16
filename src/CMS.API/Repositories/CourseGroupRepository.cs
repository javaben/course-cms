using System.Data;
using CMS.API.Infrastructure;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public sealed class CourseGroupRepository : ICourseGroupRepository
{
    private const string TableName = "CourseGroup";

    private readonly IDbConnectionFactory _factory;
    private readonly IRowAuditWriter _audit;

    public CourseGroupRepository(IDbConnectionFactory factory, IRowAuditWriter audit)
    {
        _factory = factory;
        _audit = audit;
    }

    private const string SelectColumns = @"
        SELECT g.pkid         AS Pkid,
               g.Description  AS Description
        FROM CourseGroup g";

    public async Task<IReadOnlyList<CourseGroup>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var sql = $"{SelectColumns} ORDER BY g.pkid ASC";
        var rows = await conn.QueryAsync<CourseGroup>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<CourseGroup>> QueryAsync(CourseGroupQuery query, CancellationToken ct = default)
    {
        var where = new List<string>();
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            where.Add("g.Description LIKE @Keyword");
            p.Add("@Keyword", $"%{query.Keyword.Trim()}%");
        }

        var whereClause = where.Count > 0 ? $" WHERE {string.Join(" AND ", where)}" : "";
        var sql = $"{SelectColumns}{whereClause} ORDER BY g.pkid ASC";

        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<CourseGroup>(new CommandDefinition(sql, p, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<CourseGroup?> GetByIdAsync(short pkid, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        return await LoadAsync(conn, null, pkid, ct);
    }

    /// <summary>Loads one row on the given connection/transaction (used for audit before/after snapshots).</summary>
    private static async Task<CourseGroup?> LoadAsync(
        IDbConnection conn, IDbTransaction? tx, short pkid, CancellationToken ct)
    {
        var sql = $"{SelectColumns} WHERE g.pkid = @Pkid";
        return await conn.QuerySingleOrDefaultAsync<CourseGroup>(
            new CommandDefinition(sql, new { Pkid = pkid }, tx, cancellationToken: ct));
    }

    public async Task<CourseGroup> CreateAsync(CourseGroupRequest request, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var newId = await conn.ExecuteScalarAsync<short>(new CommandDefinition(
            @"INSERT INTO CourseGroup (Description) VALUES (@Description);
              SELECT CAST(SCOPE_IDENTITY() AS smallint);",
            request, tx, cancellationToken: ct));

        var created = (await LoadAsync(conn, tx, newId, ct))!;
        await _audit.LogInsertAsync(conn, tx, TableName, created, ct);

        tx.Commit();
        return created;
    }

    public async Task<bool> UpdateAsync(CourseGroupRequest request, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var before = await LoadAsync(conn, tx, request.Pkid, ct);
        if (before is null) { tx.Rollback(); return false; }

        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE CourseGroup SET Description = @Description WHERE pkid = @Pkid;",
            request, tx, cancellationToken: ct));

        var after = (await LoadAsync(conn, tx, request.Pkid, ct))!;
        await _audit.LogUpdateAsync(conn, tx, TableName, before, after, ct);

        tx.Commit();
        return true;
    }

    public async Task<bool> DeleteAsync(short pkid, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var before = await LoadAsync(conn, tx, pkid, ct);
        if (before is null) { tx.Rollback(); return false; }

        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM CourseGroup WHERE pkid = @Pkid",
            new { Pkid = pkid }, tx, cancellationToken: ct));

        await _audit.LogDeleteAsync(conn, tx, TableName, before, ct);

        tx.Commit();
        return true;
    }
}
