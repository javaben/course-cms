namespace CMS.API.Models;

/// <summary>Search DTO for filtering the Partner (合作廠商) list.</summary>
public class PartnerQuery
{
    /// <summary>LIKE match across Name, AppKey, NameOnPartnerMenu and NameOnCourseDetailPage.</summary>
    public string? Keyword { get; set; }
}
