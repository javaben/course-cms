namespace CMS.API.Models;

/// <summary>
/// Result of resolving a Promotion2.PromoCode on the FeaturedPromoItem edit form.
/// The form sets <see cref="Pkid"/> as the FK and may pre-fill Topic/Description.
/// </summary>
public class PromotionLookup
{
    public int Pkid { get; set; }               // Promotion2.pkid
    public string PromoCode { get; set; } = ""; // 活動代碼
    public string Topic { get; set; } = "";     // 標題
    public string Description { get; set; } = ""; // 說明
}
