namespace CMS.API.Models;

/// <summary>Slim lookup row for the CourseGroup FK picker (used by Course / PartnerCourseGroup).</summary>
public class CourseGroupLookup
{
    public short Pkid { get; set; }               // 主代碼
    public string Description { get; set; } = ""; // 群組說明

    /// <summary>Display label for dropdowns (the group description).</summary>
    public string Label => Description;
}
