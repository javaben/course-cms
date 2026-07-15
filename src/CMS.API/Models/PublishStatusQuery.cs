namespace CMS.API.Models;

/// <summary>Search DTO for filtering the PublishStatus (發布狀態) list.</summary>
public class PublishStatusQuery
{
    /// <summary>LIKE match on Description.</summary>
    public string? Keyword { get; set; }

    /// <summary>Tri-state exact match on IsDraft (草稿).</summary>
    public bool? IsDraft { get; set; }

    /// <summary>Tri-state exact match on IsPublished (已發布).</summary>
    public bool? IsPublished { get; set; }

    /// <summary>Tri-state exact match on IsDiscontinued (已停用).</summary>
    public bool? IsDiscontinued { get; set; }
}
