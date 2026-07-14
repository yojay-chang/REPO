namespace CMS.API.Models;

/// <summary>
/// AppRole 使用者角色 — response model.
/// PK is <see cref="RoleId"/> (nvarchar); <see cref="Pkid"/> is the identity display code (主代碼).
/// </summary>
public class AppRole
{
    public int Pkid { get; set; }
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public int PermissionLevel { get; set; }
    public string? Description { get; set; }

    /// <summary>Number of users assigned this role (AppUserRole subquery count).</summary>
    public int UserCount { get; set; }

    /// <summary>Assigned user ids (AppUserRole). Populated on GET by id.</summary>
    public List<string> UserIds { get; set; } = [];
}
