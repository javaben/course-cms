using System.Data;
using CMS.API.Infrastructure;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public sealed class PartnerRepository : IPartnerRepository
{
    private const string TableName = "Partner";

    private readonly IDbConnectionFactory _factory;
    private readonly IRowAuditWriter _audit;

    public PartnerRepository(IDbConnectionFactory factory, IRowAuditWriter audit)
    {
        _factory = factory;
        _audit = audit;
    }

    private const string SelectColumns = @"
        SELECT p.pkid                    AS Pkid,
               p.Name                    AS Name,
               p.AppKey                  AS AppKey,
               p.NameOnPartnerMenu       AS NameOnPartnerMenu,
               p.NameOnCourseDetailPage  AS NameOnCourseDetailPage,
               p.DisplayOrder            AS DisplayOrder,
               p.ImageFilename           AS ImageFilename
        FROM Partner p";

    public async Task<IReadOnlyList<Partner>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var sql = $"{SelectColumns} ORDER BY p.DisplayOrder ASC, p.pkid ASC";
        var rows = await conn.QueryAsync<Partner>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<Partner>> QueryAsync(PartnerQuery query, CancellationToken ct = default)
    {
        var where = new List<string>();
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            where.Add(@"(p.Name LIKE @Keyword
                        OR p.AppKey LIKE @Keyword
                        OR p.NameOnPartnerMenu LIKE @Keyword
                        OR p.NameOnCourseDetailPage LIKE @Keyword)");
            p.Add("@Keyword", $"%{query.Keyword.Trim()}%");
        }

        var whereClause = where.Count > 0 ? $" WHERE {string.Join(" AND ", where)}" : "";
        var sql = $"{SelectColumns}{whereClause} ORDER BY p.DisplayOrder ASC, p.pkid ASC";

        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<Partner>(new CommandDefinition(sql, p, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<Partner?> GetByIdAsync(short pkid, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        return await LoadAsync(conn, null, pkid, ct);
    }

    /// <summary>Loads one row on the given connection/transaction (used for audit before/after snapshots).</summary>
    private static async Task<Partner?> LoadAsync(
        IDbConnection conn, IDbTransaction? tx, short pkid, CancellationToken ct)
    {
        var sql = $"{SelectColumns} WHERE p.pkid = @Pkid";
        return await conn.QuerySingleOrDefaultAsync<Partner>(
            new CommandDefinition(sql, new { Pkid = pkid }, tx, cancellationToken: ct));
    }

    public async Task<Partner> CreateAsync(PartnerRequest request, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var newId = await conn.ExecuteScalarAsync<short>(new CommandDefinition(
            @"INSERT INTO Partner (Name, AppKey, NameOnPartnerMenu, NameOnCourseDetailPage, DisplayOrder, ImageFilename)
              VALUES (@Name, @AppKey, @NameOnPartnerMenu, @NameOnCourseDetailPage, @DisplayOrder, @ImageFilename);
              SELECT CAST(SCOPE_IDENTITY() AS smallint);",
            request, tx, cancellationToken: ct));

        var created = (await LoadAsync(conn, tx, newId, ct))!;
        await _audit.LogInsertAsync(conn, tx, TableName, created, ct);

        tx.Commit();
        return created;
    }

    public async Task<bool> UpdateAsync(PartnerRequest request, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var before = await LoadAsync(conn, tx, request.Pkid, ct);
        if (before is null) { tx.Rollback(); return false; }

        await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE Partner
                 SET Name                   = @Name,
                     AppKey                 = @AppKey,
                     NameOnPartnerMenu      = @NameOnPartnerMenu,
                     NameOnCourseDetailPage = @NameOnCourseDetailPage,
                     DisplayOrder           = @DisplayOrder,
                     ImageFilename          = @ImageFilename
               WHERE pkid = @Pkid;",
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
            "DELETE FROM Partner WHERE pkid = @Pkid",
            new { Pkid = pkid }, tx, cancellationToken: ct));

        await _audit.LogDeleteAsync(conn, tx, TableName, before, ct);

        tx.Commit();
        return true;
    }
}
