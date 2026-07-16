namespace CMS.API.Models;

/// <summary>
/// One row of the cross-cutting <c>RowAudit</c> table — a single Insert/Update/Delete event against
/// any business table. Written by <see cref="Infrastructure.RowAuditWriter"/>; <see cref="Pkid"/> is
/// an IDENTITY column filled by the database, never inserted.
/// </summary>
public sealed class RowAudit
{
    public int Pkid { get; set; }                       // pkid (int IDENTITY) — DB-assigned
    public string TableName { get; set; } = "";         // e.g. "Course"
    public string UserName { get; set; } = "";          // signed-in user's UserName, or "system"
    public string PrimaryKeyValues { get; set; } = "";  // the entity's pkid, as a string
    public string ActionType { get; set; } = "";        // "Insert" | "Update" | "Delete"
    public string? ActionDesc { get; set; }             // first string prop / changed prop names
    public DateTime DateTime { get; set; }              // when the change happened
}
