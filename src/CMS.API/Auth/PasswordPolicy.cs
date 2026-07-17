namespace CMS.API.Auth;

/// <summary>
/// Complexity policy for user-chosen passwords (change-password / self-service).
/// A password is valid when it is at least 8 characters long <b>and</b> uses at least three of the
/// four character classes: uppercase, lowercase, digit, symbol. The same rule is mirrored on the
/// frontend (<c>core/auth/password-policy.ts</c>) for immediate feedback — the server is authoritative.
/// </summary>
public static class PasswordPolicy
{
    /// <summary>Minimum length.</summary>
    public const int MinLength = 8;

    /// <summary>How many of the four character classes must be present.</summary>
    public const int RequiredClasses = 3;

    /// <summary>
    /// Bilingual rejection message shown verbatim in the UI when a new password fails the policy.
    /// </summary>
    public const string ComplexityMessage =
        "密碼長度至少需 8 碼，且內容須至少包含四種字元的其中三種：大寫英文／小寫英文／數字／符號";

    /// <summary>True when <paramref name="password"/> satisfies the length and class-count rule.</summary>
    public static bool IsValid(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
            return false;

        bool hasUpper = false, hasLower = false, hasDigit = false, hasSymbol = false;
        foreach (var c in password)
        {
            if (c is >= 'A' and <= 'Z') hasUpper = true;
            else if (c is >= 'a' and <= 'z') hasLower = true;
            else if (c is >= '0' and <= '9') hasDigit = true;
            else hasSymbol = true; // anything that is not an ASCII letter or digit counts as a symbol
        }

        var classes = (hasUpper ? 1 : 0) + (hasLower ? 1 : 0) + (hasDigit ? 1 : 0) + (hasSymbol ? 1 : 0);
        return classes >= RequiredClasses;
    }
}
