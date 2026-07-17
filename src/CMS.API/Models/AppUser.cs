namespace CMS.API.Models;

/// <summary>
/// AppUser 使用者 — response model.
/// PK is <see cref="UserId"/> (nvarchar); <see cref="Pkid"/> is the identity display code (主代碼).
/// <b>PasswordHash is intentionally absent</b> — it is server-managed and never sent to the frontend.
/// </summary>
public class AppUser
{
    public int Pkid { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    /// <summary>When the password was last set (create) or reset. Read-only, server-managed.</summary>
    public DateTime? PasswordUpdatedTime { get; set; }

    /// <summary>Number of roles assigned to this user (AppUserRole subquery count).</summary>
    public int RoleCount { get; set; }

    /// <summary>Assigned role ids (AppUserRole). Populated on GET by id.</summary>
    public List<string> RoleIds { get; set; } = [];
}
