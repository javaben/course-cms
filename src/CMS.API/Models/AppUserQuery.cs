namespace CMS.API.Models;

/// <summary>Search DTO for filtering the AppUser (使用者) list.</summary>
public class AppUserQuery
{
    /// <summary>LIKE match across UserId and UserName.</summary>
    public string? Keyword { get; set; }

    /// <summary>Tri-state exact match on IsActive (啟用): null = no filter.</summary>
    public bool? IsActive { get; set; }
}
