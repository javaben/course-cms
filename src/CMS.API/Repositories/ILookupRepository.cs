using CMS.API.Models;

namespace CMS.API.Repositories;

public interface ILookupRepository
{
    /// <summary>AppUser lookup rows for the AppRole n-n picker (active users first).</summary>
    Task<IReadOnlyList<AppUserLookup>> GetAppUsersAsync(CancellationToken ct = default);

    /// <summary>PublishStatus lookup rows for the Course / Promotion FK pickers (ordered by pkid).</summary>
    Task<IReadOnlyList<PublishStatusLookup>> GetPublishStatusesAsync(CancellationToken ct = default);

    /// <summary>Partner lookup rows for the Certification / Course FK pickers (ordered by DisplayOrder).</summary>
    Task<IReadOnlyList<PartnerLookup>> GetPartnersAsync(CancellationToken ct = default);

    /// <summary>CourseGroup lookup rows for the Course / PartnerCourseGroup FK pickers (ordered by pkid).</summary>
    Task<IReadOnlyList<CourseGroupLookup>> GetCourseGroupsAsync(CancellationToken ct = default);

    /// <summary>AppRole lookup rows for the AppUser n-n picker (ordered by PermissionLevel, RoleId).</summary>
    Task<IReadOnlyList<AppRoleLookup>> GetAppRolesAsync(CancellationToken ct = default);

    /// <summary>TrainingCenter lookup rows for the FeaturedPromoItem board tabs (ordered by DisplayOrder).</summary>
    Task<IReadOnlyList<TrainingCenterLookup>> GetTrainingCentersAsync(CancellationToken ct = default);

    /// <summary>Resolve a Promotion2 by its (unique) PromoCode for the FeaturedPromoItem edit form. Null if not found.</summary>
    Task<PromotionLookup?> GetPromotionByCodeAsync(string promoCode, CancellationToken ct = default);

    /// <summary>Certification lookup rows for the Course ↔ Certification n-n picker (label = Partner.Name - Title).</summary>
    Task<IReadOnlyList<CertificationLookup>> GetCertificationsAsync(CancellationToken ct = default);

    /// <summary>JobCategory lookup rows for the Course ↔ JobCategory n-n picker (ordered by Description).</summary>
    Task<IReadOnlyList<JobCategoryLookup>> GetJobCategoriesAsync(CancellationToken ct = default);
}
