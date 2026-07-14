namespace CMS.API.Models;

/// <summary>Slim AppUser lookup row for the role-users multiselect.</summary>
public class AppUserLookup
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}
