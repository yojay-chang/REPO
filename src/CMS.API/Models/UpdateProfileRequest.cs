namespace CMS.API.Models;

/// <summary>
/// Body for <c>PUT /api/Auth/profile</c>. Only <see cref="UserName"/> is honoured — the target user is
/// always the authenticated caller (taken from the JWT), so any <see cref="UserId"/> sent here is ignored.
/// </summary>
public class UpdateProfileRequest
{
    /// <summary>
    /// Ignored by the server. Present only so a client that mistakenly sends it cannot change which
    /// account is updated — the account is resolved from the JWT, never from this value.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>The new display name. Required (non-empty after trimming).</summary>
    public string UserName { get; set; } = string.Empty;
}
