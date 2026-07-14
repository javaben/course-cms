using CMS.API.Controllers;
using CMS.API.Models;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Tests;

public class AppRolesControllerTests
{
    private static InMemoryAppRoleRepository SeededRepo() =>
        new InMemoryAppRoleRepository().Seed(
            new AppRole { Pkid = 1, RoleId = "Admin", RoleName = "Administrator", PermissionLevel = 1, Description = "系統管理員", UserIds = ["helen", "Jenny_Tsao", "miles"] },
            new AppRole { Pkid = 2, RoleId = "User", RoleName = "User", PermissionLevel = 100, Description = "一般使用者", UserIds = ["a", "b"] });

    private static AppRolesController Controller(InMemoryAppRoleRepository repo) => new(repo);

    // ---- List ----------------------------------------------------------

    [Fact]
    public async Task GetAll_returns_all_roles_ordered_by_permission_level()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var roles = Assert.IsAssignableFrom<IReadOnlyList<AppRole>>(ok.Value);
        Assert.Equal(2, roles.Count);
        Assert.Equal("Admin", roles[0].RoleId);   // PermissionLevel 1 first
        Assert.Equal("User", roles[1].RoleId);
        Assert.Equal(3, roles[0].UserCount);       // n-n count surfaced
    }

    // ---- Filter --------------------------------------------------------

    [Fact]
    public async Task Query_by_keyword_matches_roleId_name_and_description()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new AppRoleQuery { Keyword = "管理" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var roles = Assert.IsAssignableFrom<IReadOnlyList<AppRole>>(ok.Value);
        Assert.Single(roles);
        Assert.Equal("Admin", roles[0].RoleId);
    }

    [Fact]
    public async Task Query_by_permission_level_filters_exact()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new AppRoleQuery { PermissionLevel = 100 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var roles = Assert.IsAssignableFrom<IReadOnlyList<AppRole>>(ok.Value);
        Assert.Single(roles);
        Assert.Equal("User", roles[0].RoleId);
    }

    [Fact]
    public async Task Query_with_empty_filter_returns_all()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new AppRoleQuery(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var roles = Assert.IsAssignableFrom<IReadOnlyList<AppRole>>(ok.Value);
        Assert.Equal(2, roles.Count);
    }

    // ---- View ----------------------------------------------------------

    [Fact]
    public async Task GetById_returns_role_with_user_ids()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetById("Admin", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var role = Assert.IsType<AppRole>(ok.Value);
        Assert.Equal("Administrator", role.RoleName);
        Assert.Equal(3, role.UserIds.Count);
        Assert.Contains("Jenny_Tsao", role.UserIds);
    }

    [Fact]
    public async Task GetById_returns_404_when_missing()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetById("Nope", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ---- Add -----------------------------------------------------------

    [Fact]
    public async Task Create_adds_role_and_returns_201()
    {
        var repo = SeededRepo();
        var controller = Controller(repo);
        var request = new AppRoleRequest
        {
            RoleId = "Editor", RoleName = "Editor", PermissionLevel = 50,
            Description = "編輯者", UserIds = ["helen"]
        };

        var result = await controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var role = Assert.IsType<AppRole>(created.Value);
        Assert.Equal("Editor", role.RoleId);
        Assert.Equal(1, role.UserCount);
        Assert.Equal("Editor", created.RouteValues!["id"]);

        // persisted
        var fetched = await controller.GetById("Editor", CancellationToken.None);
        Assert.IsType<OkObjectResult>(fetched.Result);
    }

    [Fact]
    public async Task Create_returns_409_when_roleId_exists()
    {
        var controller = Controller(SeededRepo());
        var request = new AppRoleRequest { RoleId = "Admin", RoleName = "Dup", PermissionLevel = 1 };

        var result = await controller.Create(request, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    // ---- Edit ----------------------------------------------------------

    [Fact]
    public async Task Update_changes_fields_and_syncs_users()
    {
        var repo = SeededRepo();
        var controller = Controller(repo);
        var request = new AppRoleRequest
        {
            RoleId = "User", RoleName = "General User", PermissionLevel = 90,
            Description = "更新描述", UserIds = ["a", "b", "c"]
        };

        var result = await controller.Update(request, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);

        var fetched = await controller.GetById("User", CancellationToken.None);
        var role = Assert.IsType<AppRole>(Assert.IsType<OkObjectResult>(fetched.Result).Value);
        Assert.Equal("General User", role.RoleName);
        Assert.Equal(90, role.PermissionLevel);
        Assert.Equal(3, role.UserIds.Count);
    }

    [Fact]
    public async Task Update_returns_404_when_role_missing()
    {
        var controller = Controller(SeededRepo());
        var request = new AppRoleRequest { RoleId = "Ghost", RoleName = "x", PermissionLevel = 1 };

        var result = await controller.Update(request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    // ---- Delete --------------------------------------------------------

    [Fact]
    public async Task Delete_removes_role()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Delete("User", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.IsType<NotFoundResult>((await controller.GetById("User", CancellationToken.None)).Result);
    }

    [Fact]
    public async Task Delete_returns_404_when_missing()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Delete("Ghost", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
