using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>Write DTO for creating / updating a Partner (合作廠商).</summary>
public class PartnerRequest
{
    /// <summary>主代碼 — identifies the row on UPDATE; ignored on INSERT (IDENTITY).</summary>
    public short Pkid { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = "";                   // 廠商名稱

    [Required]
    [MaxLength(10)]
    public string AppKey { get; set; } = "";                 // 應用代碼

    [Required]
    [MaxLength(200)]
    public string NameOnPartnerMenu { get; set; } = "";      // 選單顯示名稱

    [Required]
    [MaxLength(50)]
    public string NameOnCourseDetailPage { get; set; } = ""; // 課程頁顯示名稱

    public int DisplayOrder { get; set; }                    // 顯示順序

    [MaxLength(50)]
    public string? ImageFilename { get; set; }               // 圖片檔名
}
