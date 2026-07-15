using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using CMS.API.Controllers;
using CMS.API.Infrastructure;
using CMS.API.Models;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Tests;

public class AuthControllerTests
{
    // helen: active, password "secret", two roles. miles: INACTIVE, password "hunter2", one role.
    private static InMemoryAuthRepository SeededRepo() =>
        new InMemoryAuthRepository()
            .Seed("helen", "Helen Chen", "secret", isActive: true, "Admin", "User")
            .Seed("miles", "Miles Wang", "hunter2", isActive: false, "User");

    private static AuthController Controller(InMemoryAuthRepository repo) =>
        new(repo, new FakeSigningKeyProvider());

    /// <summary>Builds a controller whose User carries a "userId" claim (as the JWT would).</summary>
    private static AuthController ControllerAs(InMemoryAuthRepository repo, string? userId)
    {
        var claims = userId is null ? new List<Claim>() : [new Claim("userId", userId)];
        var identity = new ClaimsIdentity(claims, userId is null ? null : "TestAuth");
        return new AuthController(repo, new FakeSigningKeyProvider())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
            },
        };
    }

    private static LoginResponse OkLogin(ActionResult<LoginResponse> result)
    {
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsType<LoginResponse>(ok.Value);
    }

    private static JwtSecurityToken Decode(string token) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token);

    // ---- Success -------------------------------------------------------

    [Fact]
    public async Task Login_valid_active_user_returns_profile_and_token()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Login(
            new LoginRequest { UserId = "helen", Password = "secret" }, CancellationToken.None);

        var login = OkLogin(result);
        Assert.Equal("helen", login.UserId);
        Assert.Equal("Helen Chen", login.UserName);
        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
    }

    [Fact]
    public async Task Login_issued_token_carries_identity_and_role_claims()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Login(
            new LoginRequest { UserId = "helen", Password = "secret" }, CancellationToken.None);

        var jwt = Decode(OkLogin(result).AccessToken);

        Assert.Equal("helen", jwt.Claims.Single(c => c.Type == "userId").Value);
        Assert.Equal("Helen Chen", jwt.Claims.Single(c => c.Type == "userName").Value);

        var roles = jwt.Claims.Where(c => c.Type == AuthController.RoleClaimType).Select(c => c.Value).ToList();
        Assert.Equal(2, roles.Count);
        Assert.Contains("Admin", roles);
        Assert.Contains("User", roles);
    }

    [Fact]
    public async Task Login_issued_token_expires_in_about_24h()
    {
        var controller = Controller(SeededRepo());

        var before = DateTime.UtcNow;
        var result = await controller.Login(
            new LoginRequest { UserId = "helen", Password = "secret" }, CancellationToken.None);
        var after = DateTime.UtcNow;

        var jwt = Decode(OkLogin(result).AccessToken);

        // ValidTo should sit ~24h out — bounded by the clock readings taken around the call.
        Assert.InRange(jwt.ValidTo,
            before.AddHours(24).AddSeconds(-30),
            after.AddHours(24).AddSeconds(30));
    }

    [Fact]
    public async Task Login_response_never_exposes_password_hash()
    {
        var repo = SeededRepo();
        var controller = Controller(repo);

        var result = await controller.Login(
            new LoginRequest { UserId = "helen", Password = "secret" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var json = JsonSerializer.Serialize(ok.Value);

        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PasswordHash", json);
        // The actual hash value must not leak either.
        Assert.DoesNotContain(CMS.API.Infrastructure.PasswordHasher.Sha256Hex("secret"), json);
    }

    // ---- 401 paths (all indistinguishable) -----------------------------

    [Fact]
    public async Task Login_wrong_password_returns_401()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Login(
            new LoginRequest { UserId = "helen", Password = "wrong" }, CancellationToken.None);

        AssertGenericUnauthorized(result);
    }

    [Fact]
    public async Task Login_unknown_userId_returns_401()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Login(
            new LoginRequest { UserId = "nobody", Password = "secret" }, CancellationToken.None);

        AssertGenericUnauthorized(result);
    }

    [Fact]
    public async Task Login_inactive_user_returns_401_even_with_correct_password()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Login(
            new LoginRequest { UserId = "miles", Password = "hunter2" }, CancellationToken.None);

        AssertGenericUnauthorized(result);
    }

    private static void AssertGenericUnauthorized(ActionResult<LoginResponse> result)
    {
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        // Message is generic — it must not reveal which check failed.
        var json = JsonSerializer.Serialize(unauthorized.Value);
        Assert.Contains("invalid credentials", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("active", json, StringComparison.OrdinalIgnoreCase);
    }

    // ---- Update profile (self UserName) --------------------------------

    [Fact]
    public async Task UpdateProfile_updates_username_for_the_jwt_user_only()
    {
        var repo = SeededRepo();
        var controller = ControllerAs(repo, "helen");

        var result = await controller.UpdateProfile(
            new UpdateProfileRequest { UserName = "  Helen Renamed  " }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var profile = Assert.IsType<ProfileResponse>(ok.Value);
        Assert.Equal("helen", profile.UserId);
        Assert.Equal("Helen Renamed", profile.UserName); // trimmed

        Assert.Equal("Helen Renamed", (await repo.FindByUserIdAsync("helen"))!.UserName);
        Assert.Equal("Miles Wang", (await repo.FindByUserIdAsync("miles"))!.UserName); // untouched
    }

    [Fact]
    public async Task UpdateProfile_does_not_change_roles()
    {
        var repo = SeededRepo();
        var controller = ControllerAs(repo, "helen");

        await controller.UpdateProfile(new UpdateProfileRequest { UserName = "New" }, CancellationToken.None);

        var helen = await repo.FindByUserIdAsync("helen");
        Assert.Equal(["Admin", "User"], helen!.RoleIds);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateProfile_rejects_empty_or_whitespace_username(string userName)
    {
        var repo = SeededRepo();
        var controller = ControllerAs(repo, "helen");

        var result = await controller.UpdateProfile(
            new UpdateProfileRequest { UserName = userName }, CancellationToken.None);

        // ValidationProblem → a validation error on UserName; the 400 status is asserted end-to-end
        // in AuthorizationTests (the ProblemDetailsFactory that stamps the status needs the pipeline).
        var obj = Assert.IsAssignableFrom<ObjectResult>(result.Result);
        var details = Assert.IsType<ValidationProblemDetails>(obj.Value);
        Assert.Contains(nameof(UpdateProfileRequest.UserName), details.Errors.Keys);
        Assert.Equal("Helen Chen", (await repo.FindByUserIdAsync("helen"))!.UserName); // unchanged
    }

    [Fact]
    public async Task UpdateProfile_returns_401_when_no_userId_claim()
    {
        var controller = ControllerAs(SeededRepo(), userId: null);

        var result = await controller.UpdateProfile(
            new UpdateProfileRequest { UserName = "New" }, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    // ---- Change password (self) ----------------------------------------

    private static ChangePasswordRequest ChangeReq(string current, string @new, string? confirm = null)
        => new() { CurrentPassword = current, NewPassword = @new, ConfirmNewPassword = confirm ?? @new };

    [Fact]
    public async Task ChangePassword_valid_sets_hash_of_new_and_bumps_timestamp()
    {
        var repo = SeededRepo();
        var controller = ControllerAs(repo, "helen"); // helen's current password is "secret"

        var result = await controller.ChangePassword(ChangeReq("secret", "NewPass#1"), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        var helen = await repo.FindByUserIdAsync("helen");
        Assert.Equal(PasswordHasher.Sha256Hex("NewPass#1"), helen!.PasswordHash);
        Assert.NotNull(helen.PasswordUpdatedTime);
    }

    [Fact]
    public async Task ChangePassword_wrong_current_password_changes_nothing()
    {
        var repo = SeededRepo();
        var controller = ControllerAs(repo, "helen");

        var result = await controller.ChangePassword(ChangeReq("WRONG", "NewPass#1"), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        var helen = await repo.FindByUserIdAsync("helen");
        Assert.Equal(PasswordHasher.Sha256Hex("secret"), helen!.PasswordHash); // unchanged
        Assert.Null(helen.PasswordUpdatedTime);
    }

    [Theory]
    [InlineData("Ab1#")]        // too short (< 8)
    [InlineData("abcdefgh")]    // 1 class (lower only)
    [InlineData("abcdefg1")]    // 2 classes (lower + digit)
    [InlineData("ABCDEFG1")]    // 2 classes (upper + digit)
    public async Task ChangePassword_rejects_weak_new_password_with_the_policy_message(string weak)
    {
        var repo = SeededRepo();
        var controller = ControllerAs(repo, "helen");

        var result = await controller.ChangePassword(ChangeReq("secret", weak), CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        // Read the anonymous { message } payload directly (JsonSerializer would \u-escape the CJK text).
        var message = bad.Value!.GetType().GetProperty("message")!.GetValue(bad.Value) as string;
        Assert.Equal(PasswordPolicy.ComplexityMessage, message);
        Assert.Equal(PasswordHasher.Sha256Hex("secret"), (await repo.FindByUserIdAsync("helen"))!.PasswordHash);
    }

    [Theory]
    [InlineData("Abcdefg1")]    // 3 classes, exactly 8 → OK
    [InlineData("NewPass#1")]   // 4 classes → OK
    public async Task ChangePassword_accepts_passwords_meeting_the_policy(string strong)
    {
        var repo = SeededRepo();
        var controller = ControllerAs(repo, "helen");

        var result = await controller.ChangePassword(ChangeReq("secret", strong), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task ChangePassword_rejects_mismatched_confirmation()
    {
        var repo = SeededRepo();
        var controller = ControllerAs(repo, "helen");

        var result = await controller.ChangePassword(
            ChangeReq("secret", "NewPass#1", confirm: "Different#9"), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(PasswordHasher.Sha256Hex("secret"), (await repo.FindByUserIdAsync("helen"))!.PasswordHash);
    }
}
