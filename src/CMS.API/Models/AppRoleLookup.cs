namespace CMS.API.Models;

/// <summary>Slim AppRole lookup row for the user-roles multiselect and the role filter.</summary>
public class AppRoleLookup
{
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
}
