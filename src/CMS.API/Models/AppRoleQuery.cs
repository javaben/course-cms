namespace CMS.API.Models;

/// <summary>Search DTO for filtering AppRole (角色) list.</summary>
public class AppRoleQuery
{
    /// <summary>LIKE match across RoleId, RoleName and Description.</summary>
    public string? Keyword { get; set; }

    /// <summary>Exact match on PermissionLevel (權限等級).</summary>
    public int? PermissionLevel { get; set; }
}
