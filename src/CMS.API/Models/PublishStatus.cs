namespace CMS.API.Models;

/// <summary>
/// Response model for the PublishStatus table (發布狀態).
/// The primary key <see cref="Pkid"/> is a user-supplied <c>tinyint</c> (NOT an IDENTITY
/// column), so it is editable when creating and immutable when editing.
/// </summary>
public class PublishStatus
{
    public byte Pkid { get; set; }                // 主代碼 (tinyint PK, user-supplied)
    public string Description { get; set; } = ""; // 狀態說明
    public bool IsDraft { get; set; }             // 草稿
    public bool IsPublished { get; set; }         // 已發布
    public bool IsDiscontinued { get; set; }      // 已停用
}
