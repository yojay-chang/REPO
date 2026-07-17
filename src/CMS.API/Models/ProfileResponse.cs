namespace CMS.API.Models;

/// <summary>Profile returned by <c>PUT /api/Auth/profile</c> — the authenticated user's id and updated name.</summary>
public class ProfileResponse
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}
