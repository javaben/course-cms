using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>Write DTO for creating / updating an AppRole (角色).</summary>
public class AppRoleRequest
{
    [Required]
    [MaxLength(200)]
    public string RoleId { get; set; } = "";   // 角色代碼 — PK, immutable on edit

    [Required]
    [MaxLength(200)]
    public string RoleName { get; set; } = ""; // 角色名稱

    public int PermissionLevel { get; set; } = 100; // 權限等級 (DB default 100)

    [MaxLength(400)]
    public string? Description { get; set; }    // 描述

    /// <summary>n-n: UserIds assigned to this role (via AppUserRole).</summary>
    public List<string> UserIds { get; set; } = [];
}
