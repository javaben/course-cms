using CMS.API.Models;
using CMS.API.Repositories;
using CMS.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController : ControllerBase
{
    private readonly ICourseRepository _repository;
    private readonly ILookupRepository _lookups;
    private readonly ICoursePdfService _pdfService;

    public CoursesController(
        ICourseRepository repository,
        ILookupRepository lookups,
        ICoursePdfService pdfService)
    {
        _repository = repository;
        _lookups = lookups;
        _pdfService = pdfService;
    }

    /// <summary>All courses (課程), FK labels included, ordered by DisplayOrder.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Course>>> GetAll(CancellationToken ct)
        => Ok(await _repository.GetAllAsync(ct));

    /// <summary>Filtered search.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IReadOnlyList<Course>>> Query([FromBody] CourseQuery query, CancellationToken ct)
        => Ok(await _repository.QueryAsync(query, ct));

    /// <summary>Single course by pkid — includes the two N-N id lists.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Course>> GetById(int id, CancellationToken ct)
    {
        var course = await _repository.GetByIdAsync(id, ct);
        return course is null ? NotFound() : Ok(course);
    }

    /// <summary>
    /// Course flyer as a downloadable PDF (課程宣傳單). Reuses the same GetById data path (no new
    /// course SQL) and resolves certification names via the lookup repo, then hands both to the
    /// PDF service. Inherits the app-wide JWT policy — do NOT add [AllowAnonymous].
    /// </summary>
    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> GetPdf(int id, CancellationToken ct)
    {
        var course = await _repository.GetByIdAsync(id, ct);
        if (course is null) return NotFound();

        var certLabels = await ResolveCertLabelsAsync(course, ct);
        var pdf = _pdfService.Build(course, certLabels);
        return File(pdf, "application/pdf", $"course-{id}.pdf");
    }

    /// <summary>Map the course's certification pkids to their display labels, preserving order.</summary>
    private async Task<IReadOnlyList<string>> ResolveCertLabelsAsync(Course course, CancellationToken ct)
    {
        if (course.CertificationPkids.Count == 0) return [];

        var all = await _lookups.GetCertificationsAsync(ct);
        var byPkid = all.ToDictionary(c => c.Pkid, c => c.Label);
        return course.CertificationPkids
            .Where(byPkid.ContainsKey)
            .Select(pkid => byPkid[pkid])
            .ToList();
    }

    /// <summary>Create a course. pkid is auto-generated (IDENTITY); both junctions synced in a txn.</summary>
    [HttpPost]
    public async Task<ActionResult<Course>> Create([FromBody] CourseRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var created = await _repository.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Pkid }, created);
    }

    /// <summary>Update a course. pkid (in body) identifies the record. 404 if not found.</summary>
    [HttpPut]
    public async Task<ActionResult> Update([FromBody] CourseRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var updated = await _repository.UpdateAsync(request, ct);
        return updated ? NoContent() : NotFound();
    }

    /// <summary>Delete a course by pkid. 404 if not found.</summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _repository.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
