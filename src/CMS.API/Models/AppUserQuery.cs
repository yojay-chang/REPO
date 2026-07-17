namespace CMS.API.Models;

/// <summary>Search DTO for AppUser list filtering.</summary>
public class AppUserQuery
{
    /// <summary>LIKE match on UserId, UserName.</summary>
    public string? Keyword { get; set; }

    /// <summary>Tri-state filter on IsActive (null = no filter).</summary>
    public bool? IsActive { get; set; }

    /// <summary>Restrict to users assigned this role (EXISTS on AppUserRole). Null = no filter.</summary>
    public string? RoleId { get; set; }
}
