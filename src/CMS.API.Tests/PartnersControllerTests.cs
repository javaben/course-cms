using CMS.API.Controllers;
using CMS.API.Models;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Tests;

public class PartnersControllerTests
{
    private static InMemoryPartnerRepository SeededRepo() =>
        new InMemoryPartnerRepository().Seed(
            new Partner { Pkid = 1, Name = "微軟", AppKey = "MS", NameOnPartnerMenu = "Microsoft 微軟", NameOnCourseDetailPage = "微軟", DisplayOrder = 2, ImageFilename = "ms.png" },
            new Partner { Pkid = 2, Name = "甲骨文", AppKey = "ORA", NameOnPartnerMenu = "Oracle 甲骨文", NameOnCourseDetailPage = "甲骨文", DisplayOrder = 1, ImageFilename = null });

    private static PartnersController Controller(InMemoryPartnerRepository repo) => new(repo);

    // ---- List ----------------------------------------------------------

    [Fact]
    public async Task GetAll_returns_all_rows_ordered_by_display_order()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var rows = Assert.IsAssignableFrom<IReadOnlyList<Partner>>(ok.Value);
        Assert.Equal(2, rows.Count);
        Assert.Equal("甲骨文", rows[0].Name);   // DisplayOrder 1 first
        Assert.Equal("微軟", rows[1].Name);
    }

    // ---- Filter --------------------------------------------------------

    [Fact]
    public async Task Query_by_keyword_matches_name_and_appkey()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new PartnerQuery { Keyword = "ORA" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var rows = Assert.IsAssignableFrom<IReadOnlyList<Partner>>(ok.Value);
        Assert.Single(rows);
        Assert.Equal("甲骨文", rows[0].Name);
    }

    [Fact]
    public async Task Query_with_empty_filter_returns_all()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new PartnerQuery(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var rows = Assert.IsAssignableFrom<IReadOnlyList<Partner>>(ok.Value);
        Assert.Equal(2, rows.Count);
    }

    // ---- View ----------------------------------------------------------

    [Fact]
    public async Task GetById_returns_row()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetById(1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var row = Assert.IsType<Partner>(ok.Value);
        Assert.Equal("微軟", row.Name);
        Assert.Equal("MS", row.AppKey);
    }

    [Fact]
    public async Task GetById_returns_404_when_missing()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetById(99, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ---- Add -----------------------------------------------------------

    [Fact]
    public async Task Create_adds_row_and_returns_201_with_generated_pkid()
    {
        var controller = Controller(SeededRepo());
        var request = new PartnerRequest
        {
            Name = "亞馬遜", AppKey = "AWS", NameOnPartnerMenu = "AWS 亞馬遜",
            NameOnCourseDetailPage = "亞馬遜", DisplayOrder = 3, ImageFilename = "aws.png"
        };

        var result = await controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var row = Assert.IsType<Partner>(created.Value);
        Assert.Equal("亞馬遜", row.Name);
        Assert.True(row.Pkid > 0);                        // IDENTITY assigned
        Assert.Equal(row.Pkid, created.RouteValues!["id"]);

        var fetched = await controller.GetById(row.Pkid, CancellationToken.None);
        Assert.IsType<OkObjectResult>(fetched.Result);
    }

    // ---- Edit ----------------------------------------------------------

    [Fact]
    public async Task Update_changes_fields()
    {
        var controller = Controller(SeededRepo());
        var request = new PartnerRequest
        {
            Pkid = 1, Name = "微軟公司", AppKey = "MS", NameOnPartnerMenu = "Microsoft",
            NameOnCourseDetailPage = "微軟公司", DisplayOrder = 5, ImageFilename = "ms2.png"
        };

        var result = await controller.Update(request, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);

        var fetched = await controller.GetById(1, CancellationToken.None);
        var row = Assert.IsType<Partner>(Assert.IsType<OkObjectResult>(fetched.Result).Value);
        Assert.Equal("微軟公司", row.Name);
        Assert.Equal(5, row.DisplayOrder);
    }

    [Fact]
    public async Task Update_returns_404_when_missing()
    {
        var controller = Controller(SeededRepo());
        var request = new PartnerRequest { Pkid = 99, Name = "x", AppKey = "x", NameOnPartnerMenu = "x", NameOnCourseDetailPage = "x" };

        var result = await controller.Update(request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    // ---- Delete --------------------------------------------------------

    [Fact]
    public async Task Delete_removes_row()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Delete(2, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.IsType<NotFoundResult>((await controller.GetById(2, CancellationToken.None)).Result);
    }

    [Fact]
    public async Task Delete_returns_404_when_missing()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Delete(99, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
