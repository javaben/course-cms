namespace CMS.API.Models;

/// <summary>
/// Body for <c>POST /api/Auth/change-password</c>. All three are plaintext, verified/hashed
/// server-side; no hash is ever sent to or from the client. The user is taken from the JWT.
/// </summary>
public sealed class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
    public string ConfirmNewPassword { get; set; } = "";
}
