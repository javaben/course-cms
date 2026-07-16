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
/// End-to-end tests for the global exception middleware through the real pipeline. An endpoint whose
/// repository throws returns a safe, generic 500 (no stack trace / SQL / connection details), while
/// the meaningful 401 / 403 / 400 responses keep behaving exactly as before.
/// </summary>
public class ExceptionHandlingTests : IDisposable
{
    // A secret-laden message the repository "leaks" — the response must contain none of these fragments.
    private const string SecretSql = "SELECT * FROM AppRole; Server=db-prod;User Id=sa;Password=hunter2";

    private readonly Factory _factory = new();

    public void Dispose() => _factory.Dispose();

    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ISigningKeyProvider>();
                services.AddSingleton<ISigningKeyProvider>(new FakeSigningKeyProvider());

                services.RemoveAll<IAuthRepository>();
                services.AddSingleton<IAuthRepository>(new InMemoryAuthRepository()
                    .Seed("helen", "Helen Chen", "secret", isActive: true, "Admin", "User")
                    .Seed("bob", "Bob Lin", "secret", isActive: true, "User")); // non-admin

                // GET /api/app-roles will now throw a DB-style exception carrying secrets.
                services.RemoveAll<IAppRoleRepository>();
                services.AddSingleton<IAppRoleRepository>(new ThrowingAppRoleRepository(SecretSql));
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

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client, HttpMethod method, string url, string token, object? body = null)
    {
        var req = new HttpRequestMessage(method, url) { Content = body is null ? null : JsonContent.Create(body) };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(req);
    }

    [Fact]
    public async Task Throwing_endpoint_returns_500_with_generic_message_and_no_leaked_detail()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client);

        var res = await SendAsync(client, HttpMethod.Get, "/api/app-roles", token);

        Assert.Equal(HttpStatusCode.InternalServerError, res.StatusCode);

        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains(ExceptionHandlingMiddleware.GenericMessage, body);

        // No stack trace, exception type, SQL, or connection/credential fragments leak out.
        Assert.DoesNotContain("SELECT", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hunter2", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Server=", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exception", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("at CMS.API", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Unauthenticated_request_still_returns_401_not_500()
    {
        var client = _factory.CreateClient();

        // No token → the auth middleware short-circuits with 401 before the throwing repo is ever hit.
        var res = await client.GetAsync("/api/app-roles");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Forbidden_request_still_returns_403()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "bob", "secret"); // non-admin

        var res = await SendAsync(client, HttpMethod.Post, "/api/Auth/reset-password", token,
            new ResetPasswordRequest { UserId = "helen" });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Validation_error_still_returns_400()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client);

        var res = await SendAsync(client, HttpMethod.Put, "/api/Auth/profile", token,
            new { userName = "   " }); // blank → model validation 400

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    /// <summary>An <see cref="IAppRoleRepository"/> whose every call throws a secret-laden DB error.</summary>
    private sealed class ThrowingAppRoleRepository : IAppRoleRepository
    {
        private readonly string _message;
        public ThrowingAppRoleRepository(string message) => _message = message;

        private Exception Boom() => new InvalidOperationException(_message);

        public Task<IReadOnlyList<AppRole>> GetAllAsync(CancellationToken ct = default) => throw Boom();
        public Task<IReadOnlyList<AppRole>> QueryAsync(AppRoleQuery query, CancellationToken ct = default) => throw Boom();
        public Task<AppRole?> GetByIdAsync(string roleId, CancellationToken ct = default) => throw Boom();
        public Task<bool> ExistsAsync(string roleId, CancellationToken ct = default) => throw Boom();
        public Task<AppRole> CreateAsync(AppRoleRequest request, CancellationToken ct = default) => throw Boom();
        public Task<bool> UpdateAsync(AppRoleRequest request, CancellationToken ct = default) => throw Boom();
        public Task<bool> DeleteAsync(string roleId, CancellationToken ct = default) => throw Boom();
    }
}
