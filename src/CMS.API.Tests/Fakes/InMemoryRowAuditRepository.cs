using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IRowAuditRepository"/> for controller tests — mirrors the SQL behaviour
/// (per-record filter, global filter by table/action/keyword, distinct table names, newest first)
/// without a database.
/// </summary>
public sealed class InMemoryRowAuditRepository : IRowAuditRepository
{
    private readonly List<Row> _rows = [];
    private int _seq;

    private sealed record Row(int Pkid, string TableName, string PrimaryKeyValues, RowAuditEntry Entry);

    public InMemoryRowAuditRepository Seed(string tableName, string pkid, RowAuditEntry entry)
    {
        _rows.Add(new Row(++_seq, tableName, pkid, entry));
        return this;
    }

    public Task<IReadOnlyList<RowAuditEntry>> GetForRecordAsync(
        string tableName, string pkid, CancellationToken ct = default)
    {
        IReadOnlyList<RowAuditEntry> matches = _rows
            .Where(r => r.TableName == tableName && r.PrimaryKeyValues == pkid)
            .Select(r => r.Entry)
            .OrderByDescending(e => e.DateTime)
            .ToList();
        return Task.FromResult(matches);
    }

    public Task<IReadOnlyList<RowAuditListItem>> QueryAsync(RowAuditQuery query, CancellationToken ct = default)
    {
        IEnumerable<Row> q = _rows;

        if (!string.IsNullOrWhiteSpace(query.TableName))
            q = q.Where(r => r.TableName == query.TableName.Trim());

        if (!string.IsNullOrWhiteSpace(query.ActionType))
            q = q.Where(r => r.Entry.ActionType == query.ActionType.Trim());

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            q = q.Where(r =>
                r.Entry.UserName.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || r.PrimaryKeyValues.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || (r.Entry.ActionDesc?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        IReadOnlyList<RowAuditListItem> result = q
            .OrderByDescending(r => r.Entry.DateTime)
            .ThenByDescending(r => r.Pkid)
            .Select(r => new RowAuditListItem
            {
                Pkid = r.Pkid,
                TableName = r.TableName,
                PrimaryKeyValues = r.PrimaryKeyValues,
                UserName = r.Entry.UserName,
                ActionType = r.Entry.ActionType,
                ActionDesc = r.Entry.ActionDesc,
                DateTime = r.Entry.DateTime,
            })
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<string>> GetTableNamesAsync(CancellationToken ct = default)
    {
        IReadOnlyList<string> names = _rows
            .Select(r => r.TableName)
            .Distinct()
            .OrderBy(n => n)
            .ToList();
        return Task.FromResult(names);
    }
}
