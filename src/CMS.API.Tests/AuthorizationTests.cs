using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CMS.API.Infrastructure;
using CMS.API.Models;
using CMS.API.Repositories;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CMS.API.Tests;

/// <summary>
/// End-to-end authorization tests through the real middleware pipeline (JwtBearer + the global
/// fallback policy). The DB is not available in tests, so the signing-key provider and the two
/// repositories the tests touch are swapped for fakes; everything else runs as configured in
/// <c>Program.cs</c>.
/// </summary>
public class AuthorizationTests : IDisposable
{
    // A fresh factory (→ freshly seeded in-memory repo) per test: several tests mutate user state
    // (reset/rename), so a shared fixture would leak state across tests.
    private readonly Factory _factory = new();

    public void Dispose() => _factory.Dispose();

    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                // No database in tests → provide the signing key (same instance issues + validates)
                // and in-memory repos for the two endpoints exercised here.
                services.RemoveAll<ISigningKeyProvider>();
                services.AddSingleton<ISigningKeyProvider>(new FakeSigningKeyProvider());

                services.RemoveAll<IAuthRepository>();
                services.AddSingleton<IAuthRepository>(new InMemoryAuthRepository()
                    .Seed("helen", "Helen Chen", "secret", isActive: true, "Admin", "User")
                    .Seed("bob", "Bob Lin", "secret", isActive: true, "User")); // non-admin

                services.RemoveAll<IAppRoleRepository>();
                services.AddSingleton<IAppRoleRepository>(new InMemoryAppRoleRepository().Seed(
                    new AppRole { RoleId = "Admin", RoleName = "Administrator", PermissionLevel = 1 }));

                // Course + lookup repos back the PDF endpoint's data path (no DB in tests).
                services.RemoveAll<ICourseRepository>();
                services.AddSingleton<ICourseRepository>(new InMemoryCourseRepository()
                    .WithPartners((1, "微軟"))
                    .Seed(new Course
                    {
                        Pkid = 1, Title = "Azure 基礎", CourseId = "AZ-900", PartnerPkid = 1,
                        PublishStatusPkid = 1, Hour = 40, ListPrice = 12000m, LearningCredit = 3.5m,
                        Objective = "了解雲端運算的核心概念。", CertificationPkids = [10],
                    }));

                services.RemoveAll<ILookupRepository>();
                services.AddSingleton<ILookupRepository>(new InMemoryLookupRepository()
                    .SeedCertifications(new CertificationLookup { Pkid = 10, Label = "微軟 - AZ-900" }));
            });
        }
    }

    private async Task<string> LoginAsync(HttpClient client, string userId = "helen", string password = "secret")
    {
        var res = await client.PostAsJsonAsync("/api/Auth/login", new LoginRequest { UserId = userId, Password = password });
        res.EnsureSuccessStatusCode();
        var profile = await res.Content.ReadFromJsonAsync<LoginResponse>();
        return profile!.AccessToken;
    }

    private InMemoryAuthRepository AuthRepo()
        => (InMemoryAuthRepository)_factory.Services.GetRequiredService<IAuthRepository>();

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client, HttpMethod method, string url, string token, object? body = null)
    {
        var req = new HttpRequestMessage(method, url) { Content = body is null ? null : JsonContent.Create(body) };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(req);
    }

    // ---- Protected endpoint ------------------------------------------------

    [Fact]
    public async Task Protected_endpoint_without_token_returns_401()
    {
        var client = _factory.CreateClient();

        var res = await client.GetAsync("/api/app-roles");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoint_with_valid_token_returns_200()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client);

        var req = new HttpRequestMessage(HttpMethod.Get, "/api/app-roles");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var roles = await res.Content.ReadFromJsonAsync<List<AppRole>>();
        Assert.Contains(roles!, r => r.RoleId == "Admin");
    }

    [Fact]
    public async Task Protected_endpoint_with_garbage_token_returns_401()
    {
        var client = _factory.CreateClient();

        var req = new HttpRequestMessage(HttpMethod.Get, "/api/app-roles");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");
        var res = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ---- Course PDF endpoint honors the global policy ----------------------

    [Fact]
    public async Task Course_pdf_without_token_returns_401()
    {
        var client = _factory.CreateClient();

        var res = await client.GetAsync("/api/courses/1/pdf");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Course_pdf_with_valid_token_returns_200_application_pdf()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client);

        var res = await SendAsync(client, HttpMethod.Get, "/api/courses/1/pdf", token);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("application/pdf", res.Content.Headers.ContentType?.MediaType);
        var bytes = await res.Content.ReadAsByteArrayAsync();
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public async Task Course_pdf_for_unknown_id_returns_404()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client);

        var res = await SendAsync(client, HttpMethod.Get, "/api/courses/999/pdf", token);

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    // ---- AuthController stays anonymous ------------------------------------

    [Fact]
    public async Task Auth_login_is_reachable_without_a_token()
    {
        var client = _factory.CreateClient();

        // No Authorization header at all — a valid login still succeeds, proving [AllowAnonymous].
        var res = await client.PostAsJsonAsync("/api/Auth/login",
            new LoginRequest { UserId = "helen", Password = "secret" });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var profile = await res.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.Equal("helen", profile!.UserId);
        Assert.False(string.IsNullOrWhiteSpace(profile.AccessToken));
    }

    // ---- Profile endpoint requires auth and trusts only the token -----------

    [Fact]
    public async Task Profile_update_without_token_returns_401()
    {
        var client = _factory.CreateClient();

        var res = await client.PutAsJsonAsync("/api/Auth/profile", new { userName = "Whoever" });

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Profile_update_uses_jwt_user_and_ignores_body_userId()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client); // signed in as helen

        // Body carries a bogus userId — it must be ignored; the token's user (helen) is renamed.
        var req = new HttpRequestMessage(HttpMethod.Put, "/api/Auth/profile")
        {
            Content = JsonContent.Create(new { userName = "Helen Via Api", userId = "miles" }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var profile = await res.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.Equal("helen", profile!.UserId);       // from the token, not the body's "miles"
        Assert.Equal("Helen Via Api", profile.UserName);
    }

    [Fact]
    public async Task Profile_update_with_blank_username_returns_400()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client);

        var req = new HttpRequestMessage(HttpMethod.Put, "/api/Auth/profile")
        {
            Content = JsonContent.Create(new { userName = "   " }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ---- Admin-only reset-password (role enforced by the pipeline) ----------

    [Fact]
    public async Task Reset_password_without_token_returns_401()
    {
        var client = _factory.CreateClient();

        var res = await client.PostAsJsonAsync("/api/Auth/reset-password", new ResetPasswordRequest { UserId = "bob" });

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Reset_password_by_non_admin_returns_403()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "bob", "secret"); // bob has role "User" only

        var res = await SendAsync(client, HttpMethod.Post, "/api/Auth/reset-password", token,
            new ResetPasswordRequest { UserId = "helen" });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Reset_password_by_admin_sets_default_hash_and_bumps_timestamp_returning_no_hash()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "helen", "secret"); // helen has role "Admin"

        var res = await SendAsync(client, HttpMethod.Post, "/api/Auth/reset-password", token,
            new ResetPasswordRequest { UserId = "bob" });

        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        // The response carries no password/hash...
        var body = await res.Content.ReadAsStringAsync();
        Assert.DoesNotContain("hash", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);

        // ...and bob's stored hash is now SHA256(default) with a fresh timestamp.
        var bob = await AuthRepo().FindByUserIdAsync("bob");
        Assert.Equal(PasswordHasher.Sha256Hex(InMemoryAuthRepository.DefaultPassword), bob!.PasswordHash);
        Assert.NotNull(bob.PasswordUpdatedTime);
    }

    [Fact]
    public async Task Auth_login_with_bad_credentials_returns_401_not_the_auth_challenge()
    {
        var client = _factory.CreateClient();

        // Reaches the action (anonymous) and returns the app-level generic 401 with a body —
        // distinct from the middleware challenge, which carries a WWW-Authenticate: Bearer header.
        var res = await client.PostAsJsonAsync("/api/Auth/login",
            new LoginRequest { UserId = "helen", Password = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.DoesNotContain(res.Headers.WwwAuthenticate, h => h.Scheme == "Bearer");
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("invalid credentials", body, StringComparison.OrdinalIgnoreCase);
    }
}
