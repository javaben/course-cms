using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>
/// Body for <c>POST /api/Auth/reset-password</c> (Admin only). Carries just the target user's
/// UserId — the client never sends or receives any password or hash.
/// </summary>
public sealed class ResetPasswordRequest
{
    [Required]
    public string UserId { get; set; } = "";
}
