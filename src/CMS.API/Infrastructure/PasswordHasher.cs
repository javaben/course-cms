using System.Security.Cryptography;
using System.Text;

namespace CMS.API.Infrastructure;

/// <summary>Outcome of <see cref="PasswordHasher.Verify"/>.</summary>
public enum PasswordVerificationResult
{
    /// <summary>The password does not match the stored hash.</summary>
    Failed,

    /// <summary>The password matches, and the stored hash is already in the current format.</summary>
    Success,

    /// <summary>
    /// The password matches, but the stored hash uses a superseded format (legacy SHA-256, or a lower
    /// iteration count). The caller holds the plaintext at this moment and should re-hash with
    /// <see cref="PasswordHasher.Hash"/> and persist — this is how existing rows migrate.
    /// </summary>
    SuccessRehashNeeded,
}

/// <summary>
/// Single source of truth for password hashing: PBKDF2-HMAC-SHA256 with a per-user random salt and a
/// deliberate work factor. Every flow that stores a password (AppUser create/reset, change-password)
/// calls <see cref="Hash"/>; every flow that checks one calls <see cref="Verify"/>, so a password set
/// by one is verifiable by the other.
///
/// Stored format (self-describing, so the parameters can change without a migration):
/// <c>pbkdf2-sha256$&lt;iterations&gt;$&lt;base64 salt&gt;$&lt;base64 key&gt;</c> — ~95 chars, well
/// inside <c>nvarchar(800)</c>.
///
/// Rows written before this format are bare 64-char lowercase-hex SHA-256 (see <see cref="Sha256Hex"/>).
/// <see cref="Verify"/> still accepts them and reports
/// <see cref="PasswordVerificationResult.SuccessRehashNeeded"/> so the login path can upgrade them in
/// place. Nothing writes that format any more.
/// </summary>
public static class PasswordHasher
{
    private const string Prefix = "pbkdf2-sha256";
    private const char Separator = '$';

    /// <summary>
    /// PBKDF2-HMAC-SHA256 iterations (OWASP's floor for this KDF). Raising this is safe: the value
    /// travels in the hash, and older rows re-hash on their owner's next login.
    /// </summary>
    private const int Iterations = 210_000;

    private const int SaltBytes = 16;
    private const int KeyBytes = 32; // SHA-256 native output

    private const int LegacySha256HexLength = 64;

    /// <summary>
    /// A hash of a value nobody knows, used to burn the same PBKDF2 work when there is no stored hash
    /// to check (unknown user). Without it, "no such user" would return in ~1ms while "wrong password"
    /// takes the full KDF cost — a timing oracle that turns the deliberately generic 401 into a user
    /// enumeration oracle. Computed once at startup.
    /// </summary>
    private static readonly string DummyHash = Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));

    /// <summary>
    /// Hashes <paramref name="password"/> with a fresh random salt. Two calls with the same input
    /// return different strings — that is the point, and why callers must use <see cref="Verify"/>
    /// rather than comparing hashes.
    /// </summary>
    public static string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeyBytes);

        return string.Join(Separator, Prefix, Iterations, Convert.ToBase64String(salt), Convert.ToBase64String(key));
    }

    /// <summary>
    /// Checks <paramref name="suppliedPassword"/> against <paramref name="storedHash"/>. Accepts both
    /// the current PBKDF2 format and legacy SHA-256 rows. A malformed or unrecognised stored hash is
    /// treated as <see cref="PasswordVerificationResult.Failed"/> rather than throwing — a corrupt row
    /// must not become a 500 on the login path.
    ///
    /// Callers may pass a null/empty <paramref name="storedHash"/> for a user that doesn't exist: this
    /// still burns the full KDF cost against <see cref="DummyHash"/> before failing, so an unknown user
    /// and a wrong password take the same time. Do not "optimise" that away — see <see cref="DummyHash"/>.
    /// </summary>
    public static PasswordVerificationResult Verify(string? storedHash, string? suppliedPassword)
    {
        if (suppliedPassword is null) return PasswordVerificationResult.Failed;

        if (string.IsNullOrEmpty(storedHash))
        {
            VerifyPbkdf2(DummyHash, suppliedPassword); // constant-cost decoy; result deliberately ignored
            return PasswordVerificationResult.Failed;
        }

        return storedHash.StartsWith(Prefix + Separator, StringComparison.Ordinal)
            ? VerifyPbkdf2(storedHash, suppliedPassword)
            : VerifyLegacySha256(storedHash, suppliedPassword);
    }

    private static PasswordVerificationResult VerifyPbkdf2(string storedHash, string suppliedPassword)
    {
        // pbkdf2-sha256 $ iterations $ salt $ key
        var parts = storedHash.Split(Separator);
        if (parts.Length != 4) return PasswordVerificationResult.Failed;

        if (!int.TryParse(parts[1], out var iterations) || iterations <= 0)
            return PasswordVerificationResult.Failed;

        byte[] salt, expectedKey;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expectedKey = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return PasswordVerificationResult.Failed;
        }

        if (salt.Length == 0 || expectedKey.Length == 0) return PasswordVerificationResult.Failed;

        var actualKey = Rfc2898DeriveBytes.Pbkdf2(
            suppliedPassword, salt, iterations, HashAlgorithmName.SHA256, expectedKey.Length);

        if (!CryptographicOperations.FixedTimeEquals(actualKey, expectedKey))
            return PasswordVerificationResult.Failed;

        // Correct password, but hashed under a weaker work factor than we now demand → upgrade it.
        return iterations < Iterations
            ? PasswordVerificationResult.SuccessRehashNeeded
            : PasswordVerificationResult.Success;
    }

    private static PasswordVerificationResult VerifyLegacySha256(string storedHash, string suppliedPassword)
    {
        if (storedHash.Length != LegacySha256HexLength) return PasswordVerificationResult.Failed;

        var supplied = Encoding.UTF8.GetBytes(Sha256Hex(suppliedPassword));
        var stored = Encoding.UTF8.GetBytes(storedHash.ToLowerInvariant());

        return CryptographicOperations.FixedTimeEquals(supplied, stored)
            ? PasswordVerificationResult.SuccessRehashNeeded // right password, obsolete format
            : PasswordVerificationResult.Failed;
    }

    /// <summary>
    /// Legacy hash: SHA-256 as lowercase hex. Unsalted and single-round, so it is <b>only</b> used to
    /// verify rows written before the PBKDF2 format existed (and to migrate them). Never call this to
    /// store a password — use <see cref="Hash"/>.
    /// </summary>
    internal static string Sha256Hex(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
