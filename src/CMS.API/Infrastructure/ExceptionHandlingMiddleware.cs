namespace CMS.API.Infrastructure;

/// <summary>
/// Global exception handler. Wraps the whole pipeline: any exception that bubbles up from a controller
/// or repository is logged in full server-side, and the client gets ONE consistent, safe JSON 500 —
/// never a stack trace, SQL text, or connection details.
/// <para>
/// Meaningful non-exception responses are untouched: 401 (unauthenticated), 403 (forbidden), and
/// validation/400 are produced without throwing, so they never reach the catch and pass straight
/// through.
/// </para>
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    /// <summary>The only error text ever sent to the client for an unexpected failure.</summary>
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
            // Full detail (message + stack trace) stays on the server only.
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            // If the response has already begun streaming we cannot rewrite it — let it fail.
            if (context.Response.HasStarted) throw;

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { message = GenericMessage });
        }
    }
}
