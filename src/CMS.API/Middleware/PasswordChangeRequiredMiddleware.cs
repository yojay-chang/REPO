using CMS.API.Auth;

namespace CMS.API.Middleware;

/// <summary>
/// Enforces the "first login must change the password" rule on the <b>server</b>, not merely in the UI.
///
/// A user whose stored password still equals the system default gets a token carrying
/// <see cref="JwtTokenGenerator.MustChangePasswordClaim"/> (see <c>AuthController.Login</c>). That token
/// authenticates normally, so without this middleware it would grant full API access — anyone calling the
/// API directly could skip the requirement. This middleware rejects every <c>/api</c> request made with
/// such a token except the small allow-list below, so the only thing the token can do is change the
/// password (which mints a fresh, unrestricted token).
///
/// Placed after <c>UseAuthentication</c>/<c>UseAuthorization</c> so <c>context.User</c> is populated: an
/// unauthenticated caller has already been turned away with 401 by the global fallback policy, and this
/// middleware only ever sees callers whose token was valid.
/// </summary>
public sealed class PasswordChangeRequiredMiddleware
{
    /// <summary>
    /// Machine-readable marker in the 403 body so the frontend can tell this apart from an ordinary
    /// permission denial and route the user to the change-password page.
    /// </summary>
    public const string ErrorCode = "password_change_required";

    /// <summary>Message returned with the 403 (safe to show to the user).</summary>
    public const string Message = "請先變更預設密碼。You must change your default password before continuing.";

    /// <summary>
    /// The only endpoints a restricted token may reach — exactly what the forced change-password flow
    /// needs. <c>login</c> is here for completeness (it is anonymous anyway); <c>profile</c> lets the page
    /// show who is signed in. Everything else under <c>/api</c> is refused.
    /// </summary>
    private static readonly string[] AllowedPaths =
    [
        "/api/auth/login",
        "/api/auth/change-password",
        "/api/auth/profile",
    ];

    private readonly RequestDelegate _next;

    public PasswordChangeRequiredMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsRestricted(context) && !IsAllowed(context.Request.Path))
        {
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { message = Message, code = ErrorCode });
            return;
        }

        await _next(context);
    }

    /// <summary>True when the caller presented a valid token that carries the restricting claim.</summary>
    private static bool IsRestricted(HttpContext context) =>
        context.User.Identity?.IsAuthenticated == true &&
        context.User.HasClaim(JwtTokenGenerator.MustChangePasswordClaim, "true");

    /// <summary>
    /// Non-API paths (Swagger, static files) are left alone; under <c>/api</c> only the allow-list passes.
    /// </summary>
    private static bool IsAllowed(PathString path)
    {
        if (!path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
            return true;

        return AllowedPaths.Any(allowed =>
            path.StartsWithSegments(allowed, StringComparison.OrdinalIgnoreCase));
    }
}
