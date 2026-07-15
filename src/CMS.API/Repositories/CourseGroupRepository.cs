using CMS.API.Infrastructure;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public sealed class CourseGroupRepository : ICourseGroupRepository
{
    private readonly IDbConnectionFactory _factory;

    public CourseGroupRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
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
        var sql = $"{SelectColumns} WHERE g.pkid = @Pkid";
        return await conn.QuerySingleOrDefaultAsync<CourseGroup>(
            new CommandDefinition(sql, new { Pkid = pkid }, cancellationToken: ct));
    }

    public async Task<CourseGroup> CreateAsync(CourseGroupRequest request, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var newId = await conn.ExecuteScalarAsync<short>(new CommandDefinition(
            @"INSERT INTO CourseGroup (Description) VALUES (@Description);
              SELECT CAST(SCOPE_IDENTITY() AS smallint);",
            request, cancellationToken: ct));

        return (await GetByIdAsync(newId, ct))!;
    }

    public async Task<bool> UpdateAsync(CourseGroupRequest request, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var affected = await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE CourseGroup SET Description = @Description WHERE pkid = @Pkid;",
            request, cancellationToken: ct));
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(short pkid, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var affected = await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM CourseGroup WHERE pkid = @Pkid",
            new { Pkid = pkid }, cancellationToken: ct));
        return affected > 0;
    }
}
