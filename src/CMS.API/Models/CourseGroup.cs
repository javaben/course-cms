namespace CMS.API.Models;

/// <summary>
/// Response model for the CourseGroup table (課程群組).
/// The primary key <see cref="Pkid"/> is an auto-generated <c>smallint</c> IDENTITY column.
/// </summary>
public class CourseGroup
{
    public short Pkid { get; set; }               // 主代碼 (smallint IDENTITY)
    public string Description { get; set; } = ""; // 群組說明
}
