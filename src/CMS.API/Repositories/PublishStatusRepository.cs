using CMS.API.Infrastructure;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public sealed class PublishStatusRepository : IPublishStatusRepository
{
    private readonly IDbConnectionFactory _factory;

    public PublishStatusRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    private const string SelectColumns = @"
        SELECT s.pkid            AS Pkid,
               s.Description     AS Description,
               s.IsDraft         AS IsDraft,
               s.IsPublished     AS IsPublished,
               s.IsDiscontinued  AS IsDiscontinued
        FROM PublishStatus s";

    public async Task<IReadOnlyList<PublishStatus>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var sql = $"{SelectColumns} ORDER BY s.pkid ASC";
        var rows = await conn.QueryAsync<PublishStatus>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<PublishStatus>> QueryAsync(PublishStatusQuery query, CancellationToken ct = default)
    {
        var where = new List<string>();
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            where.Add("s.Description LIKE @Keyword");
            p.Add("@Keyword", $"%{query.Keyword.Trim()}%");
        }

        if (query.IsDraft.HasValue)
        {
            where.Add("s.IsDraft = @IsDraft");
            p.Add("@IsDraft", query.IsDraft.Value);
        }

        if (query.IsPublished.HasValue)
        {
            where.Add("s.IsPublished = @IsPublished");
            p.Add("@IsPublished", query.IsPublished.Value);
        }

        if (query.IsDiscontinued.HasValue)
        {
            where.Add("s.IsDiscontinued = @IsDiscontinued");
            p.Add("@IsDiscontinued", query.IsDiscontinued.Value);
        }

        var whereClause = where.Count > 0 ? $" WHERE {string.Join(" AND ", where)}" : "";
        var sql = $"{SelectColumns}{whereClause} ORDER BY s.pkid ASC";

        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<PublishStatus>(new CommandDefinition(sql, p, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<PublishStatus?> GetByIdAsync(byte pkid, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var sql = $"{SelectColumns} WHERE s.pkid = @Pkid";
        return await conn.QuerySingleOrDefaultAsync<PublishStatus>(
            new CommandDefinition(sql, new { Pkid = pkid }, cancellationToken: ct));
    }

    public async Task<bool> ExistsAsync(byte pkid, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM PublishStatus WHERE pkid = @Pkid",
            new { Pkid = pkid }, cancellationToken: ct));
        return count > 0;
    }

    public async Task<PublishStatus> CreateAsync(PublishStatusRequest request, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO PublishStatus (pkid, Description, IsDraft, IsPublished, IsDiscontinued)
              VALUES (@Pkid, @Description, @IsDraft, @IsPublished, @IsDiscontinued);",
            request, cancellationToken: ct));

        return (await GetByIdAsync(request.Pkid, ct))!;
    }

    public async Task<bool> UpdateAsync(PublishStatusRequest request, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var affected = await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE PublishStatus
                 SET Description    = @Description,
                     IsDraft        = @IsDraft,
                     IsPublished    = @IsPublished,
                     IsDiscontinued = @IsDiscontinued
               WHERE pkid = @Pkid;",
            request, cancellationToken: ct));
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(byte pkid, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var affected = await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM PublishStatus WHERE pkid = @Pkid",
            new { Pkid = pkid }, cancellationToken: ct));
        return affected > 0;
    }
}
