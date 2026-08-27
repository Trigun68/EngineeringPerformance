using System.Text.Json;
using EngineeringPerformance.Application.Common.Exceptions;
using EngineeringPerformance.Infrastructure.Persistence;
using FluentValidation;

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
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Клиент ушёл (смена вкладки, новый переход). Не пишем 500 в лог и в ответ.
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Items["ExceptionType"] = exception.GetType().Name;
        var correlationId = context.Items.TryGetValue(CorrelationIdMiddleware.CorrelationIdItemKey, out var cid)
            ? cid?.ToString()
            : null;

        var sqlUnavailable = SqlConnectionTimeoutClassifier.IsUnavailable(exception);
        var (status, title, detail, errorCode, errors) = exception switch
        {
            _ when sqlUnavailable => (
                StatusCodes.Status503ServiceUnavailable,
                "Database temporarily unavailable.",
                "The database is busy. Retry shortly.",
                "sql_unavailable",
                (IReadOnlyDictionary<string, string[]>?)null),
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "One or more validation errors occurred.",
                "validation_failed",
                validationException.Errors
                    .GroupBy(e => string.IsNullOrWhiteSpace(e.PropertyName) ? "body" : NormalizePropertyName(e.PropertyName))
                    .ToDictionary(
                        g => g.Key,
                        g => g
                            .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage)
                            .Distinct(StringComparer.Ordinal)
                            .ToArray())),
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Resource not found.",
                exception.Message,
                "not_found",
                null),
            UnauthorizedAccessException => (
                StatusCodes.Status403Forbidden,
                "Access denied.",
                exception.Message,
                "forbidden",
                null),
            ArgumentException => (
                StatusCodes.Status400BadRequest,
                "Invalid request.",
                exception.Message,
                "invalid_request",
                null),
            InvalidOperationException => (
                StatusCodes.Status400BadRequest,
                "Invalid operation.",
                exception.Message,
                "invalid_operation",
                null),
            ConflictException conflict => (
                StatusCodes.Status409Conflict,
                "Conflict.",
                conflict.Message,
                "conflict",
                null),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                _environment.IsDevelopment() ? exception.ToString() : null,
                "internal_error",
                null)
        };

        if (sqlUnavailable)
            _logger.LogError(exception, "SQL unavailable for {Method} {Path}", context.Request.Method, context.Request.Path);
        else if (status >= 500)
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            _logger.LogWarning(exception, "Handled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new Dictionary<string, object?>
        {
            ["type"] = status == StatusCodes.Status400BadRequest && errorCode == "validation_failed"
                ? "https://engineering-performance.dev/problems/validation"
                : $"https://httpstatuses.com/{status}",
            ["title"] = title,
            ["status"] = status,
            ["detail"] = detail,
            ["traceId"] = context.TraceIdentifier,
            ["correlationId"] = correlationId,
            ["errorCode"] = errorCode,
        };

        if (errors is not null && errors.Count > 0)
            problem["errors"] = errors;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }

    private static string NormalizePropertyName(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            return "body";

        var parts = propertyName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return "body";

        return string.Join('.', parts.Select(ToCamelCase));
    }

    private static string ToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return char.IsLower(value[0])
            ? value
            : char.ToLowerInvariant(value[0]) + value[1..];
    }
}
