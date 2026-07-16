using CMS.API.Infrastructure;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public sealed class RowAuditRepository : IRowAuditRepository
{
    /// <summary>Newest-N cap on the global log so a huge audit table can't be pulled in one request.</summary>
    private const int MaxRows = 500;

    private readonly IDbConnectionFactory _factory;

    public RowAuditRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<RowAuditEntry>> GetForRecordAsync(
        string tableName, string pkid, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        // [DateTime] is bracketed (reserved word). Newest first; pkid tie-break keeps same-instant rows ordered.
        var rows = await conn.QueryAsync<RowAuditEntry>(new CommandDefinition(
            @"SELECT [DateTime]  AS [DateTime],
                     UserName    AS UserName,
                     ActionType  AS ActionType,
                     ActionDesc  AS ActionDesc
              FROM RowAudit
              WHERE TableName = @TableName AND PrimaryKeyValues = @Pkid
              ORDER BY [DateTime] DESC, pkid DESC",
            new { TableName = tableName, Pkid = pkid }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<RowAuditListItem>> QueryAsync(RowAuditQuery query, CancellationToken ct = default)
    {
        var keyword = string.IsNullOrWhiteSpace(query.Keyword) ? null : $"%{query.Keyword.Trim()}%";
        var tableName = string.IsNullOrWhiteSpace(query.TableName) ? null : query.TableName.Trim();
        var actionType = string.IsNullOrWhiteSpace(query.ActionType) ? null : query.ActionType.Trim();

        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<RowAuditListItem>(new CommandDefinition(
            @"SELECT TOP (@MaxRows)
                     pkid              AS Pkid,
                     TableName         AS TableName,
                     PrimaryKeyValues  AS PrimaryKeyValues,
                     UserName          AS UserName,
                     ActionType        AS ActionType,
                     ActionDesc        AS ActionDesc,
                     [DateTime]        AS [DateTime]
              FROM RowAudit
              WHERE (@TableName IS NULL OR TableName = @TableName)
                AND (@ActionType IS NULL OR ActionType = @ActionType)
                AND (@Keyword IS NULL
                     OR UserName LIKE @Keyword
                     OR PrimaryKeyValues LIKE @Keyword
                     OR ActionDesc LIKE @Keyword)
              ORDER BY [DateTime] DESC, pkid DESC",
            new { MaxRows, TableName = tableName, ActionType = actionType, Keyword = keyword },
            cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<string>> GetTableNamesAsync(CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<string>(new CommandDefinition(
            "SELECT DISTINCT TableName FROM RowAudit ORDER BY TableName", cancellationToken: ct));
        return rows.AsList();
    }
}
