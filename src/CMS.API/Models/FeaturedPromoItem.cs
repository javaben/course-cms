namespace CMS.API.Models;

/// <summary>
/// Response model for the FeaturedPromoItem table (上稿作業) — a scheduled promo slot on the home page.
/// The primary key <see cref="Pkid"/> is an auto-generated <c>int</c> IDENTITY column.
/// <see cref="PromoCode"/> is joined in from Promotion2 (the FK target) for display.
/// </summary>
public class FeaturedPromoItem
{
    public int Pkid { get; set; }                       // 主代碼 (int IDENTITY)
    public DateOnly ScheduleOn { get; set; }            // 上稿日期
    public short TrainingCenterPkid { get; set; }       // 訓練中心 FK
    public byte Slot { get; set; }                      // 版位 (1/2/3)
    public int PromotionPkid { get; set; }              // 活動 FK (Promotion2.pkid)
    public string PromoCode { get; set; } = "";         // 活動代碼 (Promotion2.PromoCode, joined)
    public string Topic { get; set; } = "";             // 標題
    public string Description { get; set; } = "";       // 說明
}
