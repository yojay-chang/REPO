namespace CMS.API.Models;

/// <summary>
/// User profile returned on a successful login.
/// <b>PasswordHash is intentionally absent</b> — it never leaves the backend.
/// </summary>
public class LoginResponse
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;

    /// <summary>Signed JWT access token (HS256, 24-hour lifetime).</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// True when the account still uses the system default password, so the user must change it before
    /// the token grants access to anything else. Mirrors the <c>mustChangePassword</c> claim in
    /// <see cref="AccessToken"/> — the claim, not this flag, is what the backend enforces.
    /// </summary>
    public bool MustChangePassword { get; set; }
}
