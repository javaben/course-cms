using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>
/// Write DTO for creating / updating a FeaturedPromoItem (上稿作業).
/// The client resolves the entered PromoCode to <see cref="PromotionPkid"/> via the
/// <c>GET /api/lookups/promotions/{code}</c> lookup before submitting.
/// </summary>
public class FeaturedPromoItemRequest
{
    /// <summary>主代碼 — identifies the row on UPDATE; ignored on INSERT (IDENTITY).</summary>
    public int Pkid { get; set; }

    [Required]
    public DateOnly ScheduleOn { get; set; }            // 上稿日期

    public short TrainingCenterPkid { get; set; }       // 訓練中心 FK

    [Range(1, 3)]
    public byte Slot { get; set; }                      // 版位 (1/2/3)

    public int PromotionPkid { get; set; }              // 活動 FK (resolved from PromoCode)

    [Required]
    [MaxLength(100)]
    public string Topic { get; set; } = "";             // 標題

    [Required]
    [MaxLength(300)]
    public string Description { get; set; } = "";       // 說明
}
