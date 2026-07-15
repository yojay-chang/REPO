namespace CMS.API.Models;

/// <summary>
/// Body for <c>POST /api/Auth/change-password</c>. The target account is always the authenticated
/// caller (taken from the JWT <c>userId</c> claim) — never from this body. These are plaintext inputs
/// used only for server-side verification/hashing; no password hash is ever sent to or from the client.
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>The caller's current password — must match the stored hash or the request is rejected.</summary>
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>The desired new password — must satisfy <see cref="Auth.PasswordPolicy"/>.</summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>Re-entry of the new password — must equal <see cref="NewPassword"/>.</summary>
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
