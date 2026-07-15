using CMS.API.Controllers;
using CMS.API.Models;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Tests;

public class AppUsersControllerTests
{
    private static InMemoryAppUserRepository SeededRepo() =>
        new InMemoryAppUserRepository().Seed(
            new AppUser { Pkid = 1, UserId = "helen", UserName = "Helen Chen", IsActive = true, RoleIds = ["Admin", "User"] },
            new AppUser { Pkid = 2, UserId = "miles", UserName = "Miles Wang", IsActive = false, RoleIds = ["User"] });

    private static AppUsersController Controller(InMemoryAppUserRepository repo) => new(repo);

    // ---- List ----------------------------------------------------------

    [Fact]
    public async Task GetAll_returns_all_users_active_first()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsAssignableFrom<IReadOnlyList<AppUser>>(ok.Value);
        Assert.Equal(2, users.Count);
        Assert.Equal("helen", users[0].UserId);   // IsActive DESC → active first
        Assert.Equal("miles", users[1].UserId);
        Assert.Equal(2, users[0].RoleCount);       // n-n count surfaced
    }

    // ---- Filter --------------------------------------------------------

    [Fact]
    public async Task Query_by_keyword_matches_userId_and_name()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new AppUserQuery { Keyword = "Miles" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsAssignableFrom<IReadOnlyList<AppUser>>(ok.Value);
        Assert.Single(users);
        Assert.Equal("miles", users[0].UserId);
    }

    [Fact]
    public async Task Query_by_isActive_filters_exact()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new AppUserQuery { IsActive = false }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsAssignableFrom<IReadOnlyList<AppUser>>(ok.Value);
        Assert.Single(users);
        Assert.Equal("miles", users[0].UserId);
    }

    [Fact]
    public async Task Query_with_empty_filter_returns_all()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new AppUserQuery(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsAssignableFrom<IReadOnlyList<AppUser>>(ok.Value);
        Assert.Equal(2, users.Count);
    }

    // ---- View ----------------------------------------------------------

    [Fact]
    public async Task GetById_returns_user_with_role_ids()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetById("helen", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var user = Assert.IsType<AppUser>(ok.Value);
        Assert.Equal("Helen Chen", user.UserName);
        Assert.Equal(2, user.RoleIds.Count);
        Assert.Contains("Admin", user.RoleIds);
    }

    [Fact]
    public async Task GetById_returns_404_when_missing()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetById("nobody", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ---- Add -----------------------------------------------------------

    [Fact]
    public async Task Create_adds_user_and_returns_201()
    {
        var controller = Controller(SeededRepo());
        var request = new AppUserRequest
        {
            UserId = "jenny", UserName = "Jenny Tsao", IsActive = true, RoleIds = ["User"]
        };

        var result = await controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var user = Assert.IsType<AppUser>(created.Value);
        Assert.Equal("jenny", user.UserId);
        Assert.Equal(1, user.RoleCount);
        Assert.Equal("jenny", created.RouteValues!["id"]);
        Assert.NotNull(user.PasswordUpdatedTime); // password set server-side on create

        var fetched = await controller.GetById("jenny", CancellationToken.None);
        Assert.IsType<OkObjectResult>(fetched.Result);
    }

    [Fact]
    public async Task Create_returns_409_when_userId_exists()
    {
        var controller = Controller(SeededRepo());
        var request = new AppUserRequest { UserId = "helen", UserName = "Dup" };

        var result = await controller.Create(request, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    // ---- Edit ----------------------------------------------------------

    [Fact]
    public async Task Update_changes_fields_and_syncs_roles()
    {
        var controller = Controller(SeededRepo());
        var request = new AppUserRequest
        {
            UserId = "miles", UserName = "Miles W.", IsActive = true, RoleIds = ["Admin", "User"]
        };

        var result = await controller.Update(request, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);

        var fetched = await controller.GetById("miles", CancellationToken.None);
        var user = Assert.IsType<AppUser>(Assert.IsType<OkObjectResult>(fetched.Result).Value);
        Assert.Equal("Miles W.", user.UserName);
        Assert.True(user.IsActive);
        Assert.Equal(2, user.RoleIds.Count);
    }

    [Fact]
    public async Task Update_returns_404_when_user_missing()
    {
        var controller = Controller(SeededRepo());
        var request = new AppUserRequest { UserId = "ghost", UserName = "x" };

        var result = await controller.Update(request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    // ---- Delete --------------------------------------------------------

    [Fact]
    public async Task Delete_removes_user()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Delete("miles", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.IsType<NotFoundResult>((await controller.GetById("miles", CancellationToken.None)).Result);
    }

    [Fact]
    public async Task Delete_returns_404_when_missing()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Delete("ghost", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    // ---- Reset password ------------------------------------------------

    [Fact]
    public async Task ResetPassword_returns_204_when_found()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.ResetPassword("helen", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task ResetPassword_returns_404_when_missing()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.ResetPassword("ghost", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
