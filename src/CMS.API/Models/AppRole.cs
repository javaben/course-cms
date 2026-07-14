namespace CMS.API.Models;

/// <summary>
/// Response model for the AppRole table (角色).
/// The clustered primary key is <see cref="RoleId"/> (string); <see cref="Pkid"/> is a
/// display-only IDENTITY column shown as 主代碼.
/// </summary>
public class AppRole
{
    public int Pkid { get; set; }              // 主代碼 (IDENTITY, display only)
    public string RoleId { get; set; } = "";   // 角色代碼 (primary key)
    public string RoleName { get; set; } = ""; // 角色名稱
    public int PermissionLevel { get; set; }   // 權限等級
    public string? Description { get; set; }    // 描述

    /// <summary>使用者數 — count of AppUserRole rows for this role (n-n).</summary>
    public int UserCount { get; set; }

    /// <summary>UserIds assigned to this role (populated on GET by id).</summary>
    public List<string> UserIds { get; set; } = [];
}
