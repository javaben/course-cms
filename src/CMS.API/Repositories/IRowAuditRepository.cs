using CMS.API.Models;

namespace CMS.API.Repositories;

/// <summary>Read access to the RowAudit trail — per record and across all tables.</summary>
public interface IRowAuditRepository
{
    /// <summary>
    /// The audit history for one record (matched by TableName + pkid), newest first.
    /// </summary>
    Task<IReadOnlyList<RowAuditEntry>> GetForRecordAsync(string tableName, string pkid, CancellationToken ct = default);

    /// <summary>
    /// The global audit log across all tables, filtered by <paramref name="query"/>, newest first
    /// (capped to the most recent rows).
    /// </summary>
    Task<IReadOnlyList<RowAuditListItem>> QueryAsync(RowAuditQuery query, CancellationToken ct = default);

    /// <summary>Distinct table names that appear in the audit log (for the filter picker), ordered.</summary>
    Task<IReadOnlyList<string>> GetTableNamesAsync(CancellationToken ct = default);
}
