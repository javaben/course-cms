using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>
/// Write DTO for creating / updating an AppUser (使用者).
/// PasswordHash is deliberately NOT a member — it is set server-side (from the SysConfig default
/// password on create) and only changed via the reset-password endpoint.
/// </summary>
public class AppUserRequest
{
    [Required]
    [MaxLength(200)]
    public string UserId { get; set; } = "";   // 使用者代碼 — PK, immutable on edit

    [Required]
    [MaxLength(200)]
    public string UserName { get; set; } = ""; // 使用者名稱

    public bool IsActive { get; set; } = true; // 啟用 (DB default 1)

    /// <summary>n-n: RoleIds assigned to this user (via AppUserRole).</summary>
    public List<string> RoleIds { get; set; } = [];
}
