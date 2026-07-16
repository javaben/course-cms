using CMS.API.Controllers;
using CMS.API.Models;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Tests;

public class RowAuditControllerTests
{
    private static RowAuditEntry Entry(DateTime when, string action, string user, string? desc = null)
        => new() { DateTime = when, ActionType = action, UserName = user, ActionDesc = desc };

    // Course/123 has three entries out of order; another table + another pkid are decoys.
    private static InMemoryRowAuditRepository SeededRepo() =>
        new InMemoryRowAuditRepository()
            .Seed("Course", "123", Entry(new DateTime(2026, 6, 1, 9, 0, 0), "Insert", "alice", "Intro"))
            .Seed("Course", "123", Entry(new DateTime(2026, 6, 4, 14, 30, 0), "Update", "bob", "Title"))
            .Seed("Course", "123", Entry(new DateTime(2026, 6, 2, 10, 0, 0), "Update", "alice", "Hour"))
            .Seed("Course", "999", Entry(new DateTime(2026, 6, 5, 8, 0, 0), "Insert", "carol", "Other course"))
            .Seed("Partner", "123", Entry(new DateTime(2026, 6, 6, 8, 0, 0), "Insert", "dave", "Other table"));

    [Fact]
    public async Task GetForRecord_filters_by_tableName_and_pkid_newest_first()
    {
        var controller = new RowAuditController(SeededRepo());

        var result = await controller.GetForRecord("Course", "123", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var rows = Assert.IsAssignableFrom<IReadOnlyList<RowAuditEntry>>(ok.Value);

        // Only Course/123 (not Course/999 or Partner/123), newest first.
        Assert.Equal(3, rows.Count);
        Assert.Equal(new DateTime(2026, 6, 4, 14, 30, 0), rows[0].DateTime);
        Assert.Equal("Update", rows[0].ActionType);
        Assert.Equal("bob", rows[0].UserName);
        Assert.Equal(new DateTime(2026, 6, 2, 10, 0, 0), rows[1].DateTime);
        Assert.Equal(new DateTime(2026, 6, 1, 9, 0, 0), rows[2].DateTime);
    }

    [Fact]
    public async Task GetForRecord_returns_empty_when_no_history()
    {
        var controller = new RowAuditController(SeededRepo());

        var result = await controller.GetForRecord("Course", "5555", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var rows = Assert.IsAssignableFrom<IReadOnlyList<RowAuditEntry>>(ok.Value);
        Assert.Empty(rows);
    }

    [Theory]
    [InlineData(null, "123")]
    [InlineData("Course", null)]
    [InlineData("", "123")]
    [InlineData("Course", "  ")]
    public async Task GetForRecord_requires_tableName_and_pkid(string? tableName, string? pkid)
    {
        var controller = new RowAuditController(SeededRepo());

        var result = await controller.GetForRecord(tableName, pkid, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // ---- Global log (all tables) --------------------------------------------

    [Fact]
    public async Task GetAll_returns_every_table_newest_first()
    {
        var controller = new RowAuditController(SeededRepo());

        var result = await controller.GetAll(CancellationToken.None);

        var rows = OkList(result);
        Assert.Equal(5, rows.Count); // spans Course/123, Course/999, Partner/123
        // Newest overall is Partner/123 @ 2026-06-06, then Course/999 @ 2026-06-05.
        Assert.Equal(new DateTime(2026, 6, 6, 8, 0, 0), rows[0].DateTime);
        Assert.Equal("Partner", rows[0].TableName);
        Assert.Equal(new DateTime(2026, 6, 5, 8, 0, 0), rows[1].DateTime);
        Assert.Equal("Course", rows[1].TableName);
    }

    [Fact]
    public async Task Query_filters_by_tableName_newest_first()
    {
        var controller = new RowAuditController(SeededRepo());

        var result = await controller.Query(new RowAuditQuery { TableName = "Course" }, CancellationToken.None);

        var rows = OkList(result);
        Assert.Equal(4, rows.Count); // Course/123 (x3) + Course/999
        Assert.All(rows, r => Assert.Equal("Course", r.TableName));
        Assert.True(rows[0].DateTime >= rows[^1].DateTime); // newest first
    }

    [Fact]
    public async Task Query_filters_by_actionType()
    {
        var controller = new RowAuditController(SeededRepo());

        var result = await controller.Query(new RowAuditQuery { ActionType = "Update" }, CancellationToken.None);

        var rows = OkList(result);
        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.Equal("Update", r.ActionType));
    }

    [Fact]
    public async Task Query_filters_by_keyword_across_user_pkid_and_desc()
    {
        var controller = new RowAuditController(SeededRepo());

        var result = await controller.Query(new RowAuditQuery { Keyword = "carol" }, CancellationToken.None);

        var row = Assert.Single(OkList(result));
        Assert.Equal("carol", row.UserName);
        Assert.Equal("999", row.PrimaryKeyValues);
    }

    [Fact]
    public async Task GetTables_returns_distinct_table_names_sorted()
    {
        var controller = new RowAuditController(SeededRepo());

        var result = await controller.GetTables(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var names = Assert.IsAssignableFrom<IReadOnlyList<string>>(ok.Value);
        Assert.Equal(new[] { "Course", "Partner" }, names);
    }

    private static IReadOnlyList<RowAuditListItem> OkList(ActionResult<IReadOnlyList<RowAuditListItem>> result)
    {
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsAssignableFrom<IReadOnlyList<RowAuditListItem>>(ok.Value);
    }
}
