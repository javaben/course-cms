namespace CMS.API.Models;

/// <summary>
/// Response model for the AppUser table (使用者).
/// The clustered primary key is <see cref="UserId"/> (string); <see cref="Pkid"/> is a
/// display-only IDENTITY column shown as 主代碼.
/// <see cref="PasswordHash"/> is intentionally absent — the hash is never returned to the client.
/// </summary>
public class AppUser
{
    public int Pkid { get; set; }                       // 主代碼 (IDENTITY, display only)
    public string UserId { get; set; } = "";            // 使用者代碼 (primary key)
    public string UserName { get; set; } = "";          // 使用者名稱
    public bool IsActive { get; set; } = true;          // 啟用 (DB default 1)
    public DateTime? PasswordUpdatedTime { get; set; }  // 密碼更新時間 (read-only)

    /// <summary>角色數 — count of AppUserRole rows for this user (n-n).</summary>
    public int RoleCount { get; set; }

    /// <summary>RoleIds assigned to this user (populated on GET by id).</summary>
    public List<string> RoleIds { get; set; } = [];
}
