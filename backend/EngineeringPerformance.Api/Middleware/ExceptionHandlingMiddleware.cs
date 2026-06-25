using System.Text.Json;

namespace EngineeringPerformance.Api.Middleware;

/// <summary>
/// Централизованная обработка исключений: ProblemDetails без утечки стека в Production.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Items["ExceptionType"] = exception.GetType().Name;

        var (status, title) = exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found."),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Access denied."),
            ArgumentException => (StatusCodes.Status400BadRequest, exception.Message),
            InvalidOperationException => (StatusCodes.Status400BadRequest, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        if (status >= 500)
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            _logger.LogWarning(exception, "Handled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var detail = _environment.IsDevelopment() && status >= 500
            ? exception.ToString()
            : exception.Message;

        var problem = new
        {
            type = $"https://httpstatuses.com/{status}",
            title,
            status,
            detail = status >= 500 && !_environment.IsDevelopment() ? null : detail,
            traceId = context.TraceIdentifier,
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
