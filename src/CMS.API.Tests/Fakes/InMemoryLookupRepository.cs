using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="ILookupRepository"/> for the LookupsController tests. Only the lookups exercised
/// by the FeaturedPromoItem board (TrainingCenter tabs, PromoCode resolve) carry seeded data; the rest
/// return empty lists.
/// </summary>
public sealed class InMemoryLookupRepository : ILookupRepository
{
    private readonly List<TrainingCenterLookup> _trainingCenters = [];
    private readonly List<PromotionLookup> _promotions = [];

    public InMemoryLookupRepository SeedTrainingCenters(params TrainingCenterLookup[] rows)
    {
        _trainingCenters.AddRange(rows);
        return this;
    }

    public InMemoryLookupRepository SeedPromotions(params PromotionLookup[] rows)
    {
        _promotions.AddRange(rows);
        return this;
    }

    public Task<IReadOnlyList<TrainingCenterLookup>> GetTrainingCentersAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<TrainingCenterLookup>>(
            _trainingCenters.OrderBy(t => t.Pkid).ToList());

    public Task<PromotionLookup?> GetPromotionByCodeAsync(string promoCode, CancellationToken ct = default)
        => Task.FromResult(_promotions.FirstOrDefault(p =>
            string.Equals(p.PromoCode, promoCode, StringComparison.OrdinalIgnoreCase)));

    // --- Unused by these tests -------------------------------------------
    public Task<IReadOnlyList<AppUserLookup>> GetAppUsersAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<AppUserLookup>>([]);
    public Task<IReadOnlyList<PublishStatusLookup>> GetPublishStatusesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PublishStatusLookup>>([]);
    public Task<IReadOnlyList<PartnerLookup>> GetPartnersAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PartnerLookup>>([]);
    public Task<IReadOnlyList<CourseGroupLookup>> GetCourseGroupsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CourseGroupLookup>>([]);
    public Task<IReadOnlyList<AppRoleLookup>> GetAppRolesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<AppRoleLookup>>([]);
}
