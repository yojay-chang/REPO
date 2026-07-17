namespace CMS.API.Models;

/// <summary>
/// Backend-only credential record read from <c>AppUser</c> for login verification.
/// Carries <see cref="PasswordHash"/> so the login flow can compare it — this type is
/// <b>never serialized to the frontend</b> (the controller returns <see cref="LoginResponse"/>).
/// </summary>
public class AuthCredential
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>When the password was last changed. Backend-only; never serialized to the frontend.</summary>
    public DateTime? PasswordUpdatedTime { get; set; }

    /// <summary>Role ids assigned to the user (AppUserRole) — emitted as role claims in the JWT.</summary>
    public List<string> RoleIds { get; set; } = [];
}
