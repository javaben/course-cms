namespace CMS.API.Models;

/// <summary>
/// Response model for the Partner table (合作廠商).
/// The primary key <see cref="Pkid"/> is an auto-generated <c>smallint</c> IDENTITY column.
/// </summary>
public class Partner
{
    public short Pkid { get; set; }                          // 主代碼 (smallint IDENTITY)
    public string Name { get; set; } = "";                   // 廠商名稱
    public string AppKey { get; set; } = "";                 // 應用代碼
    public string NameOnPartnerMenu { get; set; } = "";      // 選單顯示名稱
    public string NameOnCourseDetailPage { get; set; } = ""; // 課程頁顯示名稱
    public int DisplayOrder { get; set; }                    // 顯示順序
    public string? ImageFilename { get; set; }               // 圖片檔名
}
