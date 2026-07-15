namespace CMS.API.Models;

/// <summary>
/// Body for <c>POST /api/Auth/reset-password</c> (Admin only). Identifies the target account whose
/// password is reset to the SysConfig default. Only this <c>UserId</c> crosses the wire — no password
/// or hash is ever sent by, or returned to, the client.
/// </summary>
public class ResetPasswordRequest
{
    /// <summary>The UserId of the account whose password is reset to the system default.</summary>
    public string UserId { get; set; } = string.Empty;
}
