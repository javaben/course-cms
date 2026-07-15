using CMS.API.Controllers;
using CMS.API.Models;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Tests;

public class CourseGroupsControllerTests
{
    private static InMemoryCourseGroupRepository SeededRepo() =>
        new InMemoryCourseGroupRepository().Seed(
            new CourseGroup { Pkid = 1, Description = "資訊技術" },
            new CourseGroup { Pkid = 2, Description = "專案管理" });

    private static CourseGroupsController Controller(InMemoryCourseGroupRepository repo) => new(repo);

    // ---- List ----------------------------------------------------------

    [Fact]
    public async Task GetAll_returns_all_rows_ordered_by_pkid()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var rows = Assert.IsAssignableFrom<IReadOnlyList<CourseGroup>>(ok.Value);
        Assert.Equal(2, rows.Count);
        Assert.Equal(1, rows[0].Pkid);
        Assert.Equal(2, rows[1].Pkid);
    }

    // ---- Filter --------------------------------------------------------

    [Fact]
    public async Task Query_by_keyword_matches_description()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new CourseGroupQuery { Keyword = "專案" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var rows = Assert.IsAssignableFrom<IReadOnlyList<CourseGroup>>(ok.Value);
        Assert.Single(rows);
        Assert.Equal("專案管理", rows[0].Description);
    }

    [Fact]
    public async Task Query_with_empty_filter_returns_all()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new CourseGroupQuery(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var rows = Assert.IsAssignableFrom<IReadOnlyList<CourseGroup>>(ok.Value);
        Assert.Equal(2, rows.Count);
    }

    // ---- View ----------------------------------------------------------

    [Fact]
    public async Task GetById_returns_row()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetById(1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var row = Assert.IsType<CourseGroup>(ok.Value);
        Assert.Equal("資訊技術", row.Description);
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
        var request = new CourseGroupRequest { Description = "資料科學" };

        var result = await controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var row = Assert.IsType<CourseGroup>(created.Value);
        Assert.Equal("資料科學", row.Description);
        Assert.True(row.Pkid > 0);                        // IDENTITY assigned
        Assert.Equal(row.Pkid, created.RouteValues!["id"]);

        var fetched = await controller.GetById(row.Pkid, CancellationToken.None);
        Assert.IsType<OkObjectResult>(fetched.Result);
    }

    // ---- Edit ----------------------------------------------------------

    [Fact]
    public async Task Update_changes_description()
    {
        var controller = Controller(SeededRepo());
        var request = new CourseGroupRequest { Pkid = 1, Description = "資訊技術（更新）" };

        var result = await controller.Update(request, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);

        var fetched = await controller.GetById(1, CancellationToken.None);
        var row = Assert.IsType<CourseGroup>(Assert.IsType<OkObjectResult>(fetched.Result).Value);
        Assert.Equal("資訊技術（更新）", row.Description);
    }

    [Fact]
    public async Task Update_returns_404_when_missing()
    {
        var controller = Controller(SeededRepo());
        var request = new CourseGroupRequest { Pkid = 99, Description = "x" };

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
