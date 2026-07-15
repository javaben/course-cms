namespace CMS.API.Models;

/// <summary>Slim lookup row for the Partner FK picker (used by Certification / Course / PartnerCourseGroup).</summary>
public class PartnerLookup
{
    public short Pkid { get; set; }         // 主代碼
    public string Name { get; set; } = "";  // 廠商名稱

    /// <summary>Display label for dropdowns (the partner name).</summary>
    public string Label => Name;
}
