namespace CMS.API.Models;

/// <summary>Search DTO for filtering the CourseGroup (課程群組) list.</summary>
public class CourseGroupQuery
{
    /// <summary>LIKE match on Description.</summary>
    public string? Keyword { get; set; }
}
