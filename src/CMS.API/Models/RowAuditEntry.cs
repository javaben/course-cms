namespace CMS.API.Models;

/// <summary>
/// A single RowAudit history entry for one record, as returned by <c>GET /api/rowaudit</c>. Only the
/// display fields are exposed (the internal pkid / TableName / PrimaryKeyValues are omitted).
/// </summary>
public class RowAuditEntry
{
    public DateTime DateTime { get; set; }        // when the change happened
    public string UserName { get; set; } = "";    // who made it (or "system")
    public string ActionType { get; set; } = "";  // "Insert" | "Update" | "Delete"
    public string? ActionDesc { get; set; }        // first string column / changed column names
}
