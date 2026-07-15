using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>Write DTO for creating / updating a CourseGroup (課程群組).</summary>
public class CourseGroupRequest
{
    /// <summary>主代碼 — identifies the row on UPDATE; ignored on INSERT (IDENTITY).</summary>
    public short Pkid { get; set; }

    [Required]
    [MaxLength(100)]
    public string Description { get; set; } = ""; // 群組說明
}
