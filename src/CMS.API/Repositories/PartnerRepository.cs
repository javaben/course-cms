using CMS.API.Infrastructure;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public sealed class PartnerRepository : IPartnerRepository
{
    private readonly IDbConnectionFactory _factory;

    public PartnerRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
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
        var sql = $"{SelectColumns} WHERE p.pkid = @Pkid";
        return await conn.QuerySingleOrDefaultAsync<Partner>(
            new CommandDefinition(sql, new { Pkid = pkid }, cancellationToken: ct));
    }

    public async Task<Partner> CreateAsync(PartnerRequest request, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var newId = await conn.ExecuteScalarAsync<short>(new CommandDefinition(
            @"INSERT INTO Partner (Name, AppKey, NameOnPartnerMenu, NameOnCourseDetailPage, DisplayOrder, ImageFilename)
              VALUES (@Name, @AppKey, @NameOnPartnerMenu, @NameOnCourseDetailPage, @DisplayOrder, @ImageFilename);
              SELECT CAST(SCOPE_IDENTITY() AS smallint);",
            request, cancellationToken: ct));

        return (await GetByIdAsync(newId, ct))!;
    }

    public async Task<bool> UpdateAsync(PartnerRequest request, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var affected = await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE Partner
                 SET Name                   = @Name,
                     AppKey                 = @AppKey,
                     NameOnPartnerMenu      = @NameOnPartnerMenu,
                     NameOnCourseDetailPage = @NameOnCourseDetailPage,
                     DisplayOrder           = @DisplayOrder,
                     ImageFilename          = @ImageFilename
               WHERE pkid = @Pkid;",
            request, cancellationToken: ct));
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(short pkid, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var affected = await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM Partner WHERE pkid = @Pkid",
            new { Pkid = pkid }, cancellationToken: ct));
        return affected > 0;
    }
}
