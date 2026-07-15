using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>Write DTO for creating / updating a PublishStatus (發布狀態).</summary>
public class PublishStatusRequest
{
    /// <summary>主代碼 — tinyint PK, supplied on create, immutable on edit.</summary>
    public byte Pkid { get; set; }

    [Required]
    [MaxLength(50)]
    public string Description { get; set; } = ""; // 狀態說明

    public bool IsDraft { get; set; }             // 草稿
    public bool IsPublished { get; set; }         // 已發布
    public bool IsDiscontinued { get; set; }      // 已停用
}
