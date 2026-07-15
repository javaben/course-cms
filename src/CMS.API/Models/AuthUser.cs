namespace CMS.API.Models;

/// <summary>
/// Server-side credential row used only during login. Unlike <see cref="AppUser"/> this DOES carry
/// <see cref="PasswordHash"/> so the controller can verify the supplied password — it is never
/// serialized to the client (the login endpoint returns <see cref="LoginResponse"/> instead).
/// </summary>
public sealed class AuthUser
{
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public bool IsActive { get; set; }
    public string PasswordHash { get; set; } = "";

    /// <summary>When the password was last set. Not read by the login SELECT; updated by the
    /// change/reset password flows so those can be verified.</summary>
    public DateTime? PasswordUpdatedTime { get; set; }

    /// <summary>Every RoleId assigned to this user via AppUserRole — becomes a role claim in the JWT.</summary>
    public List<string> RoleIds { get; set; } = [];
}
