namespace CMS.API.Models;

/// <summary>Search DTO for AppRole list filtering.</summary>
public class AppRoleQuery
{
    /// <summary>LIKE match on RoleId, RoleName, Description.</summary>
    public string? Keyword { get; set; }

    /// <summary>Inclusive lower bound on PermissionLevel.</summary>
    public int? PermissionLevelFrom { get; set; }

    /// <summary>Inclusive upper bound on PermissionLevel.</summary>
    public int? PermissionLevelTo { get; set; }
}
