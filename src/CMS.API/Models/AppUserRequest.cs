namespace CMS.API.Models;

/// <summary>
/// Write DTO for AppUser (create/update). N-N roles carried as a list of RoleIds.
/// <b>No PasswordHash</b> — the password is server-managed (default on create, changed only via
/// the reset-password endpoint).
/// </summary>
public class AppUserRequest
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<string> RoleIds { get; set; } = [];
}
