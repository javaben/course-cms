using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/lookups")]
public class LookupsController : ControllerBase
{
    private readonly ILookupRepository _repository;

    public LookupsController(ILookupRepository repository)
    {
        _repository = repository;
    }

    /// <summary>AppUser lookup list for the AppRole n-n picker.</summary>
    [HttpGet("app-users")]
    public async Task<ActionResult<IReadOnlyList<AppUserLookup>>> GetAppUsers(CancellationToken ct)
        => Ok(await _repository.GetAppUsersAsync(ct));

    /// <summary>PublishStatus lookup list for the Course / Promotion FK pickers.</summary>
    [HttpGet("publish-statuses")]
    public async Task<ActionResult<IReadOnlyList<PublishStatusLookup>>> GetPublishStatuses(CancellationToken ct)
        => Ok(await _repository.GetPublishStatusesAsync(ct));

    /// <summary>Partner lookup list for the Certification / Course FK pickers.</summary>
    [HttpGet("partners")]
    public async Task<ActionResult<IReadOnlyList<PartnerLookup>>> GetPartners(CancellationToken ct)
        => Ok(await _repository.GetPartnersAsync(ct));

    /// <summary>CourseGroup lookup list for the Course / PartnerCourseGroup FK pickers.</summary>
    [HttpGet("course-groups")]
    public async Task<ActionResult<IReadOnlyList<CourseGroupLookup>>> GetCourseGroups(CancellationToken ct)
        => Ok(await _repository.GetCourseGroupsAsync(ct));

    /// <summary>AppRole lookup list for the AppUser n-n picker.</summary>
    [HttpGet("app-roles")]
    public async Task<ActionResult<IReadOnlyList<AppRoleLookup>>> GetAppRoles(CancellationToken ct)
        => Ok(await _repository.GetAppRolesAsync(ct));

    /// <summary>TrainingCenter lookup list for the FeaturedPromoItem board tabs.</summary>
    [HttpGet("training-centers")]
    public async Task<ActionResult<IReadOnlyList<TrainingCenterLookup>>> GetTrainingCenters(CancellationToken ct)
        => Ok(await _repository.GetTrainingCentersAsync(ct));

    /// <summary>Resolve a Promotion2 by PromoCode for the FeaturedPromoItem edit form. 404 if not found.</summary>
    [HttpGet("promotions/{code}")]
    public async Task<ActionResult<PromotionLookup>> GetPromotionByCode(string code, CancellationToken ct)
    {
        var promo = await _repository.GetPromotionByCodeAsync(code, ct);
        return promo is null ? NotFound() : Ok(promo);
    }
}
