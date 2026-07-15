namespace CMS.API.Models;

/// <summary>Slim lookup row for the PublishStatus FK picker (used by Course / Promotion).</summary>
public class PublishStatusLookup
{
    public byte Pkid { get; set; }                // 主代碼
    public string Description { get; set; } = ""; // 狀態說明

    /// <summary>Display label for dropdowns, e.g. "1 - 已發布".</summary>
    public string Label => $"{Pkid} - {Description}";
}
