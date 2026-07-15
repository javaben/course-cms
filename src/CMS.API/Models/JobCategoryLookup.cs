namespace CMS.API.Models;

/// <summary>Slim lookup row for the Course ↔ JobCategory n-n picker.</summary>
public class JobCategoryLookup
{
    public short Pkid { get; set; }                // 主代碼
    public string Description { get; set; } = "";  // 職務類別說明

    /// <summary>Display label for the picker (the description).</summary>
    public string Label => Description;
}
