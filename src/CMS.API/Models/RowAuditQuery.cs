namespace CMS.API.Models;

/// <summary>Filter for the global RowAudit log (all optional; null = unfiltered).</summary>
public class RowAuditQuery
{
    public string? TableName { get; set; }   // exact table match
    public string? ActionType { get; set; }  // "Insert" | "Update" | "Delete"
    public string? Keyword { get; set; }      // matches UserName / PrimaryKeyValues / ActionDesc
}
