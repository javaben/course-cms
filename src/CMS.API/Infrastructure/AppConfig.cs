using System.Text.Json;

namespace CMS.API.Infrastructure;

/// <summary>
/// Reads required string properties out of the <c>SysConfig['appConfig']</c> JSON blob
/// (e.g. <c>symmetricSecurityKey</c>, <c>defaultPassword</c>). One place to parse that blob so the
/// signing-key provider and the password flows stay consistent. Read at runtime — never hard-coded.
/// </summary>
public static class AppConfig
{
    /// <summary>
    /// Extracts a non-empty string property from the appConfig JSON. Throws a descriptive
    /// <see cref="InvalidOperationException"/> when the blob is missing/invalid or the property is
    /// absent/blank (a misconfiguration → surfaces as 500, not a silent default).
    /// </summary>
    public static string GetRequiredProperty(string? configValueJson, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(configValueJson))
            throw new InvalidOperationException("SysConfig['appConfig'] 未設定。");

        try
        {
            using var doc = JsonDocument.Parse(configValueJson);
            if (doc.RootElement.TryGetProperty(propertyName, out var prop) &&
                prop.ValueKind == JsonValueKind.String &&
                prop.GetString() is { Length: > 0 } value)
            {
                return value;
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("SysConfig['appConfig'] 不是有效的 JSON。", ex);
        }

        throw new InvalidOperationException($"appConfig.{propertyName} 未設定。");
    }
}
