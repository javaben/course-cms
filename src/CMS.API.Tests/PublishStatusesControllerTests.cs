using CMS.API.Controllers;
using CMS.API.Models;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Tests;

public class PublishStatusesControllerTests
{
    private static InMemoryPublishStatusRepository SeededRepo() =>
        new InMemoryPublishStatusRepository().Seed(
            new PublishStatus { Pkid = 1, Description = "草稿", IsDraft = true, IsPublished = false, IsDiscontinued = false },
            new PublishStatus { Pkid = 2, Description = "已發布", IsDraft = false, IsPublished = true, IsDiscontinued = false },
            new PublishStatus { Pkid = 3, Description = "已停用", IsDraft = false, IsPublished = false, IsDiscontinued = true });

    private static PublishStatusesController Controller(InMemoryPublishStatusRepository repo) => new(repo);

    // ---- List ----------------------------------------------------------

    [Fact]
    public async Task GetAll_returns_all_rows_ordered_by_pkid()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var rows = Assert.IsAssignableFrom<IReadOnlyList<PublishStatus>>(ok.Value);
        Assert.Equal(3, rows.Count);
        Assert.Equal(1, rows[0].Pkid);
        Assert.Equal(3, rows[2].Pkid);
    }

    // ---- Filter --------------------------------------------------------

    [Fact]
    public async Task Query_by_keyword_matches_description()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new PublishStatusQuery { Keyword = "發布" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var rows = Assert.IsAssignableFrom<IReadOnlyList<PublishStatus>>(ok.Value);
        Assert.Single(rows);
        Assert.Equal(2, rows[0].Pkid);
    }

    [Fact]
    public async Task Query_by_isPublished_filters_exact()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new PublishStatusQuery { IsPublished = true }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var rows = Assert.IsAssignableFrom<IReadOnlyList<PublishStatus>>(ok.Value);
        Assert.Single(rows);
        Assert.Equal("已發布", rows[0].Description);
    }

    [Fact]
    public async Task Query_with_empty_filter_returns_all()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new PublishStatusQuery(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var rows = Assert.IsAssignableFrom<IReadOnlyList<PublishStatus>>(ok.Value);
        Assert.Equal(3, rows.Count);
    }

    // ---- View ----------------------------------------------------------

    [Fact]
    public async Task GetById_returns_row()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetById(2, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var row = Assert.IsType<PublishStatus>(ok.Value);
        Assert.Equal("已發布", row.Description);
        Assert.True(row.IsPublished);
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
    public async Task Create_adds_row_and_returns_201()
    {
        var controller = Controller(SeededRepo());
        var request = new PublishStatusRequest
        {
            Pkid = 4, Description = "審核中", IsDraft = true, IsPublished = false, IsDiscontinued = false
        };

        var result = await controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var row = Assert.IsType<PublishStatus>(created.Value);
        Assert.Equal((byte)4, row.Pkid);
        Assert.Equal((byte)4, created.RouteValues!["id"]);

        var fetched = await controller.GetById(4, CancellationToken.None);
        Assert.IsType<OkObjectResult>(fetched.Result);
    }

    [Fact]
    public async Task Create_returns_409_when_pkid_exists()
    {
        var controller = Controller(SeededRepo());
        var request = new PublishStatusRequest { Pkid = 1, Description = "重複" };

        var result = await controller.Create(request, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    // ---- Edit ----------------------------------------------------------

    [Fact]
    public async Task Update_changes_fields()
    {
        var controller = Controller(SeededRepo());
        var request = new PublishStatusRequest
        {
            Pkid = 1, Description = "草稿（更新）", IsDraft = true, IsPublished = false, IsDiscontinued = false
        };

        var result = await controller.Update(request, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);

        var fetched = await controller.GetById(1, CancellationToken.None);
        var row = Assert.IsType<PublishStatus>(Assert.IsType<OkObjectResult>(fetched.Result).Value);
        Assert.Equal("草稿（更新）", row.Description);
    }

    [Fact]
    public async Task Update_returns_404_when_missing()
    {
        var controller = Controller(SeededRepo());
        var request = new PublishStatusRequest { Pkid = 99, Description = "x" };

        var result = await controller.Update(request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    // ---- Delete --------------------------------------------------------

    [Fact]
    public async Task Delete_removes_row()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Delete(3, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.IsType<NotFoundResult>((await controller.GetById(3, CancellationToken.None)).Result);
    }

    [Fact]
    public async Task Delete_returns_404_when_missing()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Delete(99, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
