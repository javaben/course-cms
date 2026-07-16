namespace CMS.API.Models;

/// <summary>
/// One row of the global RowAudit log (all tables), as returned by <c>GET /api/rowaudit/all</c> /
/// <c>POST /api/rowaudit/query</c>. Unlike <see cref="RowAuditEntry"/> (per-record) this carries the
/// TableName + PrimaryKeyValues so a single list can span every table.
/// </summary>
public class RowAuditListItem
{
    public int Pkid { get; set; }                       // the audit row's own IDENTITY pkid (table dataKey)
    public string TableName { get; set; } = "";         // e.g. "Course"
    public string PrimaryKeyValues { get; set; } = "";  // the changed record's pkid, as a string
    public string UserName { get; set; } = "";          // who made the change (or "system")
    public string ActionType { get; set; } = "";        // "Insert" | "Update" | "Delete"
    public string? ActionDesc { get; set; }              // first string column / changed column names
    public DateTime DateTime { get; set; }              // when the change happened
}
