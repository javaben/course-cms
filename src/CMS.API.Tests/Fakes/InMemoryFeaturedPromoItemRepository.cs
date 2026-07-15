using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IFeaturedPromoItemRepository"/> for controller tests — mirrors the SQL behaviour
/// (TrainingCenter + Monday–Sunday week filter, ScheduleOn/Slot ordering, the Promotion2 PromoCode join,
/// IDENTITY pkid, the unique (date, center, slot) constraint, and the slot-swap move) without a database.
/// </summary>
public sealed class InMemoryFeaturedPromoItemRepository : IFeaturedPromoItemRepository
{
    private readonly List<FeaturedPromoItem> _rows = [];
    private readonly Dictionary<int, string> _promoCodes = new(); // Promotion_pkid -> PromoCode (mirrors the JOIN)
    private int _nextPkid = 1;

    /// <summary>Registers the PromoCode a Promotion_pkid resolves to (stands in for the Promotion2 join).</summary>
    public InMemoryFeaturedPromoItemRepository WithPromotions(params (int pkid, string code)[] promos)
    {
        foreach (var (pkid, code) in promos) _promoCodes[pkid] = code;
        return this;
    }

    public InMemoryFeaturedPromoItemRepository Seed(params FeaturedPromoItem[] rows)
    {
        foreach (var r in rows)
        {
            if (r.Pkid == 0) r.Pkid = _nextPkid;
            _nextPkid = Math.Max(_nextPkid, r.Pkid) + 1;
            if (string.IsNullOrEmpty(r.PromoCode) && _promoCodes.TryGetValue(r.PromotionPkid, out var code))
                r.PromoCode = code;
            _rows.Add(r);
        }
        return this;
    }

    public Task<IReadOnlyList<FeaturedPromoItem>> QueryAsync(FeaturedPromoItemQuery query, CancellationToken ct = default)
    {
        IEnumerable<FeaturedPromoItem> q = _rows;

        if (query.TrainingCenterPkid is { } tc)
            q = q.Where(r => r.TrainingCenterPkid == tc);

        if (query.WeekStart is { } weekStart)
        {
            var weekEnd = weekStart.AddDays(6);
            q = q.Where(r => r.ScheduleOn >= weekStart && r.ScheduleOn <= weekEnd);
        }

        var ordered = q.OrderBy(r => r.ScheduleOn).ThenBy(r => r.Slot).ToList();
        return Task.FromResult<IReadOnlyList<FeaturedPromoItem>>(ordered);
    }

    public Task<FeaturedPromoItem?> GetByIdAsync(int pkid, CancellationToken ct = default)
        => Task.FromResult(Find(pkid));

    public Task<bool> SlotTakenAsync(DateOnly scheduleOn, short trainingCenterPkid, byte slot, int? excludePkid = null, CancellationToken ct = default)
        => Task.FromResult(_rows.Any(r =>
            r.ScheduleOn == scheduleOn &&
            r.TrainingCenterPkid == trainingCenterPkid &&
            r.Slot == slot &&
            (excludePkid is null || r.Pkid != excludePkid)));

    public Task<FeaturedPromoItem> CreateAsync(FeaturedPromoItemRequest request, CancellationToken ct = default)
    {
        var row = new FeaturedPromoItem
        {
            Pkid = _nextPkid++,
            ScheduleOn = request.ScheduleOn,
            TrainingCenterPkid = request.TrainingCenterPkid,
            Slot = request.Slot,
            PromotionPkid = request.PromotionPkid,
            PromoCode = _promoCodes.GetValueOrDefault(request.PromotionPkid, ""),
            Topic = request.Topic,
            Description = request.Description,
        };
        _rows.Add(row);
        return Task.FromResult(row);
    }

    public Task<bool> UpdateAsync(FeaturedPromoItemRequest request, CancellationToken ct = default)
    {
        var row = Find(request.Pkid);
        if (row is null) return Task.FromResult(false);

        row.ScheduleOn = request.ScheduleOn;
        row.TrainingCenterPkid = request.TrainingCenterPkid;
        row.Slot = request.Slot;
        row.PromotionPkid = request.PromotionPkid;
        row.PromoCode = _promoCodes.GetValueOrDefault(request.PromotionPkid, row.PromoCode);
        row.Topic = request.Topic;
        row.Description = request.Description;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int pkid, CancellationToken ct = default)
    {
        var row = Find(pkid);
        if (row is null) return Task.FromResult(false);
        _rows.Remove(row);
        return Task.FromResult(true);
    }

    public Task<bool> MoveSlotAsync(int pkid, int direction, CancellationToken ct = default)
    {
        if (direction != 1 && direction != -1) return Task.FromResult(false);

        var row = Find(pkid);
        if (row is null) return Task.FromResult(false);

        var target = row.Slot + direction;
        if (target is < 1 or > 3) return Task.FromResult(false);
        var targetSlot = (byte)target;

        var neighbour = _rows.FirstOrDefault(r =>
            r.ScheduleOn == row.ScheduleOn &&
            r.TrainingCenterPkid == row.TrainingCenterPkid &&
            r.Slot == targetSlot);

        if (neighbour is not null) neighbour.Slot = row.Slot; // swap
        row.Slot = targetSlot;
        return Task.FromResult(true);
    }

    private FeaturedPromoItem? Find(int pkid) => _rows.FirstOrDefault(r => r.Pkid == pkid);
}
