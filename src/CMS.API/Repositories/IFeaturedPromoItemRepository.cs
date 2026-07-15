using CMS.API.Models;

namespace CMS.API.Repositories;

public interface IFeaturedPromoItemRepository
{
    /// <summary>Filtered board query — by TrainingCenter tab and Monday–Sunday week; PromoCode joined in.</summary>
    Task<IReadOnlyList<FeaturedPromoItem>> QueryAsync(FeaturedPromoItemQuery query, CancellationToken ct = default);

    Task<FeaturedPromoItem?> GetByIdAsync(int pkid, CancellationToken ct = default);

    /// <summary>True if a row already occupies (ScheduleOn, TrainingCenter, Slot), optionally excluding one pkid.</summary>
    Task<bool> SlotTakenAsync(DateOnly scheduleOn, short trainingCenterPkid, byte slot, int? excludePkid = null, CancellationToken ct = default);

    /// <summary>Inserts the item (pkid auto-generated). Returns the created record (with PromoCode).</summary>
    Task<FeaturedPromoItem> CreateAsync(FeaturedPromoItemRequest request, CancellationToken ct = default);

    /// <summary>Updates the item (pkid immutable). Returns false if it does not exist.</summary>
    Task<bool> UpdateAsync(FeaturedPromoItemRequest request, CancellationToken ct = default);

    Task<bool> DeleteAsync(int pkid, CancellationToken ct = default);

    /// <summary>
    /// Moves a row between slots (direction +1 = down 1→2→3, -1 = up 3→2→1). If the target slot on the
    /// same day/center is occupied the two rows swap slots (single transaction). Returns false when the
    /// row is missing or the target slot is out of range (must stay within 1–3).
    /// </summary>
    Task<bool> MoveSlotAsync(int pkid, int direction, CancellationToken ct = default);
}
