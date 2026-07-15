using System.Text;
using Dapper;
using Microsoft.IdentityModel.Tokens;

namespace CMS.API.Infrastructure;

/// <summary>
/// Single source of the JWT signing secret: <c>SysConfig['appConfig'].symmetricSecurityKey</c>.
/// Both the issuing side (<c>AuthController</c>) and the validation side (JwtBearer) resolve the key
/// through this provider, so a token this app signs is a token this app accepts. Read at runtime —
/// never hard-coded.
/// </summary>
public interface ISigningKeyProvider
{
    /// <summary>The raw secret string.</summary>
    string GetSigningKey();

    /// <summary>The secret as a <see cref="SymmetricSecurityKey"/> for signing/validation.</summary>
    SymmetricSecurityKey GetSecurityKey();
}

public sealed class SigningKeyProvider : ISigningKeyProvider
{
    private readonly IDbConnectionFactory _factory;
    private readonly Lock _gate = new();
    private string? _cached;

    public SigningKeyProvider(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public SymmetricSecurityKey GetSecurityKey()
        => new(Encoding.UTF8.GetBytes(GetSigningKey()));

    public string GetSigningKey()
    {
        if (_cached is not null) return _cached;
        lock (_gate)
        {
            _cached ??= Load();
            return _cached;
        }
    }

    // The key rarely changes and every request would otherwise re-read it, so cache after first load.
    // The connection factory is async-only; this runs once, off any request sync-context, so the
    // blocking wait here is safe.
    private string Load()
    {
        using var conn = _factory.CreateOpenConnectionAsync().GetAwaiter().GetResult();

        var json = conn.ExecuteScalar<string?>(
            "SELECT configValue FROM SysConfig WHERE configKey = 'appConfig'");

        return AppConfig.GetRequiredProperty(json, "symmetricSecurityKey");
    }
}
