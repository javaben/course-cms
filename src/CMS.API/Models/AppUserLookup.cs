namespace CMS.API.Models;

/// <summary>Slim lookup row for AppUser, used as the n-n target in the AppRole form.</summary>
public class AppUserLookup
{
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";

    /// <summary>Display label, e.g. "Jenny_Tsao (Jenny_Tsao)".</summary>
    public string Label => $"{UserName} ({UserId})";
}
