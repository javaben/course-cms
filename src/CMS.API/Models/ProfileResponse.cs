namespace CMS.API.Models;

/// <summary>Result of a profile update — the (canonical, trimmed) UserName for the JWT's user.</summary>
public sealed class ProfileResponse
{
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
}
