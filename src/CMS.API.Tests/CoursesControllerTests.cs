using CMS.API.Controllers;
using CMS.API.Models;
using CMS.API.Services;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Tests;

public class CoursesControllerTests
{
    private static readonly DateOnly On = new(2026, 3, 1);
    private static readonly DateOnly Off = new(2036, 3, 1);

    private static InMemoryCourseRepository SeededRepo() =>
        new InMemoryCourseRepository()
            .WithPartners((1, "微軟"), (2, "甲骨文"))
            .WithCourseGroups((5, "雲端"))
            .WithPublishStatuses((1, "草稿"), (2, "已發布"))
            .Seed(
                new Course
                {
                    Pkid = 1, Title = "Azure 基礎", CourseId = "AZ-900", ProdCourseId = "P-AZ900",
                    FriendlyUrl = "azure-900", DisplayOrder = 2, PartnerPkid = 1, CourseGroupPkid = 5,
                    PublishStatusPkid = 2, ScheduleOn = On, ScheduleOff = Off, Hour = 14, ListPrice = 12000,
                    LearningCredit = 3.5m, CanRepeat = true,
                    CertificationPkids = [10, 11], JobCategoryPkids = [7],
                },
                new Course
                {
                    Pkid = 2, Title = "Oracle SQL", CourseId = "ORA-SQL", ProdCourseId = "P-ORASQL",
                    FriendlyUrl = "oracle-sql", DisplayOrder = 1, PartnerPkid = 2, CourseGroupPkid = null,
                    PublishStatusPkid = 1, ScheduleOn = On, ScheduleOff = Off, Hour = 21, ListPrice = 18000,
                    LearningCredit = 5m, CanRepeat = false,
                });

    // Certifications the seeded course 1 points at (pkids 10, 11), plus an unrelated row.
    private static InMemoryLookupRepository SeededLookups() =>
        new InMemoryLookupRepository().SeedCertifications(
            new CertificationLookup { Pkid = 10, Label = "微軟 - AZ-900" },
            new CertificationLookup { Pkid = 11, Label = "微軟 - AZ-104" },
            new CertificationLookup { Pkid = 99, Label = "甲骨文 - OCP" });

    private static readonly ICoursePdfService Pdf = new CoursePdfService("https://test.example.com");

    private static CoursesController Controller(InMemoryCourseRepository repo) => new(repo, SeededLookups(), Pdf);

    private static CoursesController Controller(InMemoryCourseRepository repo, InMemoryLookupRepository lookups)
        => new(repo, lookups, Pdf);

    private static IReadOnlyList<Course> Rows(ActionResult<IReadOnlyList<Course>> result)
    {
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsAssignableFrom<IReadOnlyList<Course>>(ok.Value);
    }

    private static CourseRequest NewRequest() => new()
    {
        Title = "Google Cloud 入門", CourseId = "GCP-01", ProdCourseId = "P-GCP01",
        FriendlyUrl = "gcp-intro", DisplayOrder = 3, PartnerPkid = 1, CourseGroupPkid = 5,
        PublishStatusPkid = 2, ScheduleOn = On, ScheduleOff = Off, Hour = 7, ListPrice = 6000,
        LearningCredit = 2m, CanRepeat = true,
        CertificationPkids = [20, 21], JobCategoryPkids = [8, 9],
    };

    // ---- List ----------------------------------------------------------

    [Fact]
    public async Task GetAll_orders_by_display_order_and_fills_fk_labels()
    {
        var result = await Controller(SeededRepo()).GetAll(CancellationToken.None);

        var rows = Rows(result);
        Assert.Equal(2, rows.Count);
        Assert.Equal("Oracle SQL", rows[0].Title);          // DisplayOrder 1 first
        Assert.Equal("微軟", rows[1].Partner?.Name);          // FK nav label filled
        Assert.Equal("已發布", rows[1].PublishStatus?.Description);
        Assert.Null(rows[0].CourseGroup);                    // null FK → null nav
    }

    // ---- Filter --------------------------------------------------------

    [Fact]
    public async Task Query_by_keyword_matches_course_id()
    {
        var result = await Controller(SeededRepo()).Query(new CourseQuery { Keyword = "ORA" }, CancellationToken.None);

        var rows = Rows(result);
        Assert.Single(rows);
        Assert.Equal("Oracle SQL", rows[0].Title);
    }

    [Fact]
    public async Task Query_by_partner_filters()
    {
        var result = await Controller(SeededRepo()).Query(new CourseQuery { PartnerPkid = 1 }, CancellationToken.None);

        var rows = Rows(result);
        Assert.Single(rows);
        Assert.Equal("Azure 基礎", rows[0].Title);
    }

    [Fact]
    public async Task Query_by_publish_status_filters()
    {
        var result = await Controller(SeededRepo()).Query(new CourseQuery { PublishStatusPkid = 1 }, CancellationToken.None);

        var rows = Rows(result);
        Assert.Single(rows);
        Assert.Equal("Oracle SQL", rows[0].Title);
    }

    [Fact]
    public async Task Query_by_can_repeat_filters()
    {
        var result = await Controller(SeededRepo()).Query(new CourseQuery { CanRepeat = true }, CancellationToken.None);

        var rows = Rows(result);
        Assert.Single(rows);
        Assert.Equal("Azure 基礎", rows[0].Title);
    }

    [Fact]
    public async Task Query_by_schedule_on_range_filters()
    {
        // A window that starts after the seeded ScheduleOn excludes every row.
        var result = await Controller(SeededRepo()).Query(
            new CourseQuery { ScheduleOnFrom = new DateOnly(2026, 6, 1) }, CancellationToken.None);

        Assert.Empty(Rows(result));
    }

    // ---- View ----------------------------------------------------------

    [Fact]
    public async Task GetById_returns_course_with_nn_lists()
    {
        var result = await Controller(SeededRepo()).GetById(1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var row = Assert.IsType<Course>(ok.Value);
        Assert.Equal("Azure 基礎", row.Title);
        Assert.Equal(new[] { 10, 11 }, row.CertificationPkids);
        Assert.Equal(new short[] { 7 }, row.JobCategoryPkids);
    }

    [Fact]
    public async Task GetById_returns_404_when_missing()
    {
        var result = await Controller(SeededRepo()).GetById(99, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ---- PDF flyer -----------------------------------------------------

    [Fact]
    public async Task GetPdf_returns_application_pdf_file_for_known_course()
    {
        var result = await Controller(SeededRepo()).GetPdf(1, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("course-1.pdf", file.FileDownloadName);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(file.FileContents, 0, 4));
        Assert.True(file.FileContents.Length > 15_000, "CJK flyer should embed glyph data (size floor)");
    }

    [Fact]
    public async Task GetPdf_returns_404_when_missing()
    {
        var result = await Controller(SeededRepo()).GetPdf(99, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetPdf_still_succeeds_when_certifications_lookup_is_empty()
    {
        // No cert rows seeded → labels resolve to empty; the flyer must still render (section hidden).
        var result = await Controller(SeededRepo(), new InMemoryLookupRepository()).GetPdf(1, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.True(file.FileContents.Length > 0);
    }

    // ---- Add -----------------------------------------------------------

    [Fact]
    public async Task Create_returns_201_with_generated_pkid_and_persists_nn()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Create(NewRequest(), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var row = Assert.IsType<Course>(created.Value);
        Assert.True(row.Pkid > 0);
        Assert.Equal(row.Pkid, created.RouteValues!["id"]);

        var fetched = await controller.GetById(row.Pkid, CancellationToken.None);
        var stored = Assert.IsType<Course>(Assert.IsType<OkObjectResult>(fetched.Result).Value);
        Assert.Equal(new[] { 20, 21 }, stored.CertificationPkids);
        Assert.Equal(new short[] { 8, 9 }, stored.JobCategoryPkids);
    }

    // ---- Edit ----------------------------------------------------------

    [Fact]
    public async Task Update_changes_scalars_and_resyncs_nn()
    {
        var controller = Controller(SeededRepo());
        var request = new CourseRequest
        {
            Pkid = 1, Title = "Azure 進階", CourseId = "AZ-900", ProdCourseId = "P-AZ900",
            FriendlyUrl = "azure-900", DisplayOrder = 2, PartnerPkid = 1, CourseGroupPkid = 5,
            PublishStatusPkid = 2, ScheduleOn = On, ScheduleOff = Off, Hour = 28, ListPrice = 15000,
            LearningCredit = 4m, CanRepeat = true,
            CertificationPkids = [11], JobCategoryPkids = [],   // dropped 10 and cleared job cats
        };

        var result = await controller.Update(request, CancellationToken.None);
        Assert.IsType<NoContentResult>(result);

        var fetched = await controller.GetById(1, CancellationToken.None);
        var row = Assert.IsType<Course>(Assert.IsType<OkObjectResult>(fetched.Result).Value);
        Assert.Equal("Azure 進階", row.Title);
        Assert.Equal(28, row.Hour);
        Assert.Equal(new[] { 11 }, row.CertificationPkids);
        Assert.Empty(row.JobCategoryPkids);
    }

    [Fact]
    public async Task Update_returns_404_when_missing()
    {
        var request = NewRequest();
        request.Pkid = 99;

        var result = await Controller(SeededRepo()).Update(request, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    // ---- Delete --------------------------------------------------------

    [Fact]
    public async Task Delete_removes_row()
    {
        var controller = Controller(SeededRepo());

        Assert.IsType<NoContentResult>(await controller.Delete(2, CancellationToken.None));
        Assert.IsType<NotFoundResult>((await controller.GetById(2, CancellationToken.None)).Result);
    }

    [Fact]
    public async Task Delete_returns_404_when_missing()
    {
        var result = await Controller(SeededRepo()).Delete(99, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }
}
