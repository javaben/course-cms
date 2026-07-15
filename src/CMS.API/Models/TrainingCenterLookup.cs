namespace CMS.API.Models;

/// <summary>Slim lookup row for the TrainingCenter tabs on the FeaturedPromoItem board.</summary>
public class TrainingCenterLookup
{
    public short Pkid { get; set; }         // 主代碼
    public string Name { get; set; } = "";  // 訓練中心名稱

    /// <summary>Display label for the tab (the center name).</summary>
    public string Label => Name;
}
