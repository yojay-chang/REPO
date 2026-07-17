using CMS.API.Auth;
using CMS.API.Data;
using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // Login must be reachable without a token; every other controller requires auth.
public class AuthController : ControllerBase
{
    private readonly IAuthRepository _repository;

    public AuthController(IAuthRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Authenticate a user against the AppUser table and, on success, issue a 24-hour JWT.
    /// Any failed check returns a generic 401 that does not reveal which part failed.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        // Generic error — never disclose whether the user, password, or active flag was the problem.
        var invalid = Unauthorized(new { message = "invalid credentials" });

        if (request is null ||
            string.IsNullOrWhiteSpace(request.UserId) ||
            string.IsNullOrEmpty(request.Password))
        {
            return invalid;
        }

        var credential = await _repository.GetCredentialAsync(request.UserId);
        if (credential is null || !credential.IsActive)
            return invalid;

        if (credential.PasswordHash != PasswordHasher.Sha256(request.Password))
            return invalid;

        // Still on the system default password (a first login, or an Admin just reset the account) →
        // issue a restricted token: it authenticates, but PasswordChangeRequiredMiddleware refuses
        // everything except the change-password flow until a new password is set.
        var mustChangePassword = credential.PasswordHash == await _repository.GetDefaultPasswordHashAsync();

        var signingKey = await _repository.GetSigningKeyAsync();
        var accessToken = JwtTokenGenerator.Generate(credential, signingKey, mustChangePassword);

        return Ok(new LoginResponse
        {
            UserId = credential.UserId,
            UserName = credential.UserName,
            AccessToken = accessToken,
            MustChangePassword = mustChangePassword
        });
    }

    /// <summary>
    /// Update the signed-in user's own display name. The account is taken from the JWT
    /// (<c>userId</c> claim) — never from the request body, so <see cref="UpdateProfileRequest.UserId"/>
    /// is ignored and neither the UserId nor the roles can be changed here.
    /// </summary>
    [Authorize] // Overrides the class-level [AllowAnonymous]: a valid token is required.
    [HttpPut("profile")]
    public async Task<ActionResult<ProfileResponse>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var userName = request?.UserName?.Trim();
        if (string.IsNullOrWhiteSpace(userName))
            return BadRequest(new { message = "UserName is required." });

        await _repository.UpdateUserNameAsync(userId, userName);

        return Ok(new ProfileResponse { UserId = userId, UserName = userName });
    }

    /// <summary>
    /// Change the signed-in user's own password. The account is taken from the JWT (<c>userId</c> claim),
    /// never from the body. The flow: (1) the current password must match the stored hash; (2) the new
    /// password must satisfy <see cref="PasswordPolicy"/>; (3) the new password and its confirmation must
    /// match, and must not be the system default (which would leave the account still "must change").
    /// On success the stored <c>PasswordHash</c> is set to SHA-256(new), <c>PasswordUpdatedTime</c> is
    /// stamped, and a <b>fresh token without</b> <see cref="JwtTokenGenerator.MustChangePasswordClaim"/>
    /// is returned — a user forced here by that claim would otherwise stay locked out with their old
    /// token. No password hash is ever accepted from or returned to the client.
    /// </summary>
    [Authorize] // Overrides the class-level [AllowAnonymous]: a valid token is required.
    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (request is null ||
            string.IsNullOrEmpty(request.CurrentPassword) ||
            string.IsNullOrEmpty(request.NewPassword) ||
            string.IsNullOrEmpty(request.ConfirmNewPassword))
        {
            return BadRequest(new { message = "所有欄位皆為必填。All fields are required." });
        }

        var credential = await _repository.GetCredentialAsync(userId);
        if (credential is null)
            return Unauthorized();

        // 1. The current password must match the stored hash — otherwise nothing changes.
        if (credential.PasswordHash != PasswordHasher.Sha256(request.CurrentPassword))
            return BadRequest(new { message = "目前密碼不正確。Current password is incorrect." });

        // 2. New-password complexity.
        if (!PasswordPolicy.IsValid(request.NewPassword))
            return BadRequest(new { message = PasswordPolicy.ComplexityMessage });

        // 3. New password and confirmation must match.
        if (request.NewPassword != request.ConfirmNewPassword)
            return BadRequest(new { message = "新密碼與確認密碼不一致。New password and confirmation do not match." });

        var newPasswordHash = PasswordHasher.Sha256(request.NewPassword);

        // 4. The new password must not be the system default — that is exactly the state the forced
        //    change exists to leave, and accepting it would bounce the user straight back here.
        if (newPasswordHash == await _repository.GetDefaultPasswordHashAsync())
            return BadRequest(new { message = "新密碼不可與系統預設密碼相同。The new password must not be the system default." });

        // 5. Persist — store only the hash; stamp the update time (handled by the repository).
        await _repository.UpdatePasswordAsync(userId, newPasswordHash);

        // 6. Mint a fresh token. The caller's current token may carry MustChangePasswordClaim, which the
        //    middleware keeps enforcing until the token is replaced — so hand back one without it.
        var signingKey = await _repository.GetSigningKeyAsync();
        var accessToken = JwtTokenGenerator.Generate(credential, signingKey);

        return Ok(new { message = "密碼已更新。Password changed.", accessToken });
    }

    /// <summary>
    /// Reset another user's password to the system default. <b>Admin only</b>: an anonymous caller gets
    /// 401 and an authenticated non-Admin caller gets 403. The target account is the <c>UserId</c> in the
    /// body; the default password is read from <c>SysConfig['appConfig'].defaultPassword</c> at runtime and
    /// the target's <c>PasswordHash</c> is set to SHA-256(default) with <c>PasswordUpdatedTime</c> stamped.
    /// No password or hash is ever accepted from, or returned to, the client.
    /// </summary>
    /// <remarks>
    /// The class-level <c>[AllowAnonymous]</c> bypasses a declarative <c>[Authorize]</c> on this action, so
    /// authentication and the Admin role are enforced explicitly here — by the JWT's role claim
    /// (<see cref="System.Security.Claims.ClaimsPrincipal.IsInRole"/>), not merely by hiding the UI button.
    /// </remarks>
    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();
        if (!User.IsInRole("Admin"))
            return Forbid();

        var userId = request?.UserId?.Trim();
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest(new { message = "UserId 為必填。UserId is required." });

        var reset = await _repository.ResetPasswordToDefaultAsync(userId);
        if (!reset)
            return NotFound(new { message = $"找不到使用者「{userId}」。User not found." });

        return Ok(new { message = "密碼已重設為系統預設值。Password reset to the system default." });
    }
}
