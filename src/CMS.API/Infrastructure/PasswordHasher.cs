using System.Security.Cryptography;
using System.Text;

namespace CMS.API.Infrastructure;

/// <summary>
/// Single source of truth for password hashing: SHA-256 rendered as lowercase hex (64 chars,
/// fits <c>nvarchar(800)</c>). Both the AppUser create/reset flow and the login credential check
/// hash the same way, so a password set by one is verifiable by the other.
/// </summary>
public static class PasswordHasher
{
    public static string Sha256Hex(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
