using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>
/// Login credentials posted to <c>POST /api/Auth/login</c>. The password is checked against
/// <see cref="AppUser.PasswordHash"/> (PBKDF2, salted) server-side and never stored on this model.
/// </summary>
public sealed class LoginRequest
{
    [Required]
    public string UserId { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}
