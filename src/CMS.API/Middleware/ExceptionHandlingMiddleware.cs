namespace CMS.API.Middleware;

/// <summary>
/// Catches any unhandled exception thrown downstream (controllers, repositories, Dapper/SQL) and turns
/// it into ONE consistent JSON error response: HTTP 500 with a generic, safe message. The full exception
/// (message + stack trace) is logged server-side only — the stack trace, SQL text, and connection details
/// never reach the client.
///
/// Placed first in the pipeline so it wraps everything downstream. It only reacts to *thrown* exceptions,
/// so the meaningful status codes that are set without throwing — 401 (unauthenticated), 403 (forbidden),
/// and validation/400 — flow through untouched.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    /// <summary>The single generic message returned for every unexpected error.</summary>
    public const string GenericMessage = "An unexpected error occurred.";

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Full detail server-side only (message + stack trace).
            _logger.LogError(ex, "Unhandled exception processing {Method} {Path}",
                context.Request.Method, context.Request.Path);

            // If the response has already begun streaming we can't replace it — let it surface.
            if (context.Response.HasStarted)
                throw;

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { message = GenericMessage });
        }
    }
}
