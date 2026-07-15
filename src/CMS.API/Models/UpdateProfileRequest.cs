using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>
/// Body for <c>PUT /api/Auth/profile</c>. Deliberately carries ONLY the editable UserName — no
/// UserId and no roles — so those cannot be changed through this endpoint. The identity is taken
/// from the JWT.
/// </summary>
public sealed class UpdateProfileRequest
{
    [Required]
    public string UserName { get; set; } = "";
}
