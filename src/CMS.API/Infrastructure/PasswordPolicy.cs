namespace CMS.API.Infrastructure;

/// <summary>
/// New-password complexity rule (shared wording with the Angular client): at least 8 characters and
/// at least 3 of the 4 classes — uppercase, lowercase, digit, symbol.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;

    /// <summary>The exact (bilingual-in-UI) message returned when <see cref="IsComplexEnough"/> fails.</summary>
    public const string ComplexityMessage =
        "密碼長度至少需 8 碼，且內容須至少包含四種字元的其中三種：大寫英文／小寫英文／數字／符號";

    public static bool IsComplexEnough(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinLength) return false;

        var classes = 0;
        if (password.Any(char.IsUpper)) classes++;
        if (password.Any(char.IsLower)) classes++;
        if (password.Any(char.IsDigit)) classes++;
        if (password.Any(c => !char.IsLetterOrDigit(c))) classes++; // symbol = anything not letter/digit
        return classes >= 3;
    }
}
