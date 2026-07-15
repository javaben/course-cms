using System.Text;
using CMS.API.Infrastructure;
using Microsoft.IdentityModel.Tokens;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// Test <see cref="ISigningKeyProvider"/> with a fixed key — stands in for
/// <c>SysConfig['appConfig'].symmetricSecurityKey</c> so tests need no database. The same instance
/// (same key) is used to both issue and validate tokens, mirroring production.
/// </summary>
public sealed class FakeSigningKeyProvider : ISigningKeyProvider
{
    // Long enough (>= 256 bits) to satisfy HMAC-SHA256's key-size requirement.
    public const string Key = "cms-test-signing-key-please-change-0123456789abcdef";

    private readonly string _key;

    public FakeSigningKeyProvider(string? key = null) => _key = key ?? Key;

    public string GetSigningKey() => _key;

    public SymmetricSecurityKey GetSecurityKey() => new(Encoding.UTF8.GetBytes(_key));
}
