using CMS.API.Infrastructure;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public sealed class LookupRepository : ILookupRepository
{
    private readonly IDbConnectionFactory _factory;

    public LookupRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<AppUserLookup>> GetAppUsersAsync(CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<AppUserLookup>(new CommandDefinition(
            @"SELECT UserId, UserName
              FROM AppUser
              ORDER BY IsActive DESC, UserName ASC",
            cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<PublishStatusLookup>> GetPublishStatusesAsync(CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<PublishStatusLookup>(new CommandDefinition(
            @"SELECT pkid AS Pkid, Description AS Description
              FROM PublishStatus
              ORDER BY pkid ASC",
            cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<PartnerLookup>> GetPartnersAsync(CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<PartnerLookup>(new CommandDefinition(
            @"SELECT pkid AS Pkid, Name AS Name
              FROM Partner
              ORDER BY DisplayOrder ASC, pkid ASC",
            cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<CourseGroupLookup>> GetCourseGroupsAsync(CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<CourseGroupLookup>(new CommandDefinition(
            @"SELECT pkid AS Pkid, Description AS Description
              FROM CourseGroup
              ORDER BY pkid ASC",
            cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<AppRoleLookup>> GetAppRolesAsync(CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<AppRoleLookup>(new CommandDefinition(
            @"SELECT RoleId AS RoleId, RoleName AS RoleName
              FROM AppRole
              ORDER BY PermissionLevel ASC, RoleId ASC",
            cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<TrainingCenterLookup>> GetTrainingCentersAsync(CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<TrainingCenterLookup>(new CommandDefinition(
            @"SELECT pkid AS Pkid, Name AS Name
              FROM TrainingCenter
              ORDER BY DisplayOrder ASC, pkid ASC",
            cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<PromotionLookup?> GetPromotionByCodeAsync(string promoCode, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<PromotionLookup>(new CommandDefinition(
            @"SELECT pkid AS Pkid, PromoCode AS PromoCode, Topic AS Topic, Description AS Description
              FROM Promotion2
              WHERE PromoCode = @PromoCode",
            new { PromoCode = promoCode }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<CertificationLookup>> GetCertificationsAsync(CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        // Certification.Title is nchar(100) → RTRIM; label prefixed with the owning Partner's name.
        var rows = await conn.QueryAsync<CertificationLookup>(new CommandDefinition(
            @"SELECT c.pkid AS Pkid,
                     p.Name + ' - ' + RTRIM(c.Title) AS Label
              FROM Certification c
              JOIN Partner p ON p.pkid = c.Partner_pkid
              ORDER BY p.Name ASC, c.Title ASC",
            cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<JobCategoryLookup>> GetJobCategoriesAsync(CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<JobCategoryLookup>(new CommandDefinition(
            @"SELECT pkid AS Pkid, Description AS Description
              FROM JobCategory
              ORDER BY Description ASC",
            cancellationToken: ct));
        return rows.AsList();
    }
}
