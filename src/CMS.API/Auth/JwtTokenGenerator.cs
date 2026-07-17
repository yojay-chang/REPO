using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CMS.API.Models;
using Microsoft.IdentityModel.Tokens;

namespace CMS.API.Auth;

/// <summary>Builds the signed access token returned on a successful login.</summary>
public static class JwtTokenGenerator
{
    /// <summary>Token lifetime — the token expires 24 hours after issue.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    /// <summary>
    /// Claim written (as <c>"true"</c>) when the account still uses the system default password. A token
    /// carrying it is restricted: <c>PasswordChangeRequiredMiddleware</c> rejects every endpoint except
    /// the ones needed to change the password. Absent on a normal token.
    /// </summary>
    public const string MustChangePasswordClaim = "mustChangePassword";

    /// <summary>
    /// Build an HS256-signed JWT for <paramref name="credential"/>, signed with
    /// <paramref name="signingKey"/> (the SysConfig <c>symmetricSecurityKey</c>). Claims: the
    /// UserId, the UserName, one role claim per assigned RoleId, and — when
    /// <paramref name="mustChangePassword"/> is true — the restricting
    /// <see cref="MustChangePasswordClaim"/>.
    /// </summary>
    public static string Generate(AuthCredential credential, string signingKey, bool mustChangePassword = false)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new("userId", credential.UserId),
            new("userName", credential.UserName),
        };
        claims.AddRange(credential.RoleIds.Select(roleId => new Claim(ClaimTypes.Role, roleId)));

        if (mustChangePassword)
            claims.Add(new Claim(MustChangePasswordClaim, "true"));

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            claims: claims,
            notBefore: now,
            expires: now.Add(Lifetime),
            signingCredentials: signingCredentials);

        // Clear the outbound map so ClaimTypes.Role is written verbatim (deterministic claim types).
        var handler = new JwtSecurityTokenHandler();
        handler.OutboundClaimTypeMap.Clear();
        return handler.WriteToken(token);
    }
}
