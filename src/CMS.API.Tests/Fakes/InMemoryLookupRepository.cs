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
    private readonly List<CertificationLookup> _certifications = [];
    private readonly List<JobCategoryLookup> _jobCategories = [];

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

    public InMemoryLookupRepository SeedCertifications(params CertificationLookup[] rows)
    {
        _certifications.AddRange(rows);
        return this;
    }

    public InMemoryLookupRepository SeedJobCategories(params JobCategoryLookup[] rows)
    {
        _jobCategories.AddRange(rows);
        return this;
    }

    public Task<IReadOnlyList<TrainingCenterLookup>> GetTrainingCentersAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<TrainingCenterLookup>>(
            _trainingCenters.OrderBy(t => t.Pkid).ToList());

    public Task<PromotionLookup?> GetPromotionByCodeAsync(string promoCode, CancellationToken ct = default)
        => Task.FromResult(_promotions.FirstOrDefault(p =>
            string.Equals(p.PromoCode, promoCode, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<CertificationLookup>> GetCertificationsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CertificationLookup>>(
            _certifications.OrderBy(c => c.Label, StringComparer.Ordinal).ToList());

    public Task<IReadOnlyList<JobCategoryLookup>> GetJobCategoriesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<JobCategoryLookup>>(
            _jobCategories.OrderBy(j => j.Description, StringComparer.Ordinal).ToList());

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
