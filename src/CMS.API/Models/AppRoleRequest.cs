namespace CMS.API.Models;

/// <summary>Write DTO for AppRole (create/update). N-N users carried as a list of UserIds.</summary>
public class AppRoleRequest
{
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public int PermissionLevel { get; set; } = 100;
    public string? Description { get; set; }
    public List<string> UserIds { get; set; } = [];
}
