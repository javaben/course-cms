namespace CMS.API.Models;

/// <summary>Slim lookup row for AppRole, used as the n-n target in the AppUser form.</summary>
public class AppRoleLookup
{
    public string RoleId { get; set; } = "";
    public string RoleName { get; set; } = "";

    /// <summary>Display label, e.g. "Administrator (Admin)".</summary>
    public string Label => $"{RoleName} ({RoleId})";
}
