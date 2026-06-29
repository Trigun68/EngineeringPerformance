using System.Threading.RateLimiting;
using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Api.Configuration;
using EngineeringPerformance.Api.Middleware;
using EngineeringPerformance.Api.Validators;
using EngineeringPerformance.Application.Validators;
using EngineeringPerformance.Infrastructure;
using EngineeringPerformance.Infrastructure.Auth;
using EngineeringPerformance.Infrastructure.Logging;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using CorsOptions = EngineeringPerformance.Api.Configuration.CorsOptions;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    ValidateRequiredConfiguration(builder.Configuration, builder.Environment);

    builder.Services.Configure<SpnegoOptions>(builder.Configuration.GetSection(SpnegoOptions.SectionName));
    builder.Services.Configure<DevWindowsAuthOptions>(builder.Configuration.GetSection(DevWindowsAuthOptions.SectionName));
    builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });

    // FluentValidation: авто-регистрация валидаторов из Assembly (S1-01)
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<DirectoryWriteRequestValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problem = new ValidationProblemDetails(context.ModelState)
            {
                Type = "https://engineering-performance.dev/problems/validation",
                Title = "Validation failed.",
                Status = StatusCodes.Status400BadRequest,
                Detail = "One or more validation errors occurred.",
                Instance = context.HttpContext.Request.Path,
            };
            problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            problem.Extensions["errorCode"] = "validation_failed";
            problem.Errors.Clear();

            foreach (var (key, value) in context.ModelState)
            {
                if (value?.Errors.Count is null or 0)
                    continue;

                var normalizedKey = NormalizeModelStateKey(key);
                var messages = value.Errors
                    .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage)
                    .ToArray();

                if (problem.Errors.TryGetValue(normalizedKey, out var existing))
                {
                    problem.Errors[normalizedKey] = existing.Concat(messages).Distinct(StringComparer.Ordinal).ToArray();
                }
                else
                {
                    problem.Errors[normalizedKey] = messages;
                }
            }

            return new BadRequestObjectResult(problem);
        };
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Health checks (конкретные чеки добавляются в AddInfrastructure)
    builder.Services.AddHealthChecks();

    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Host.UseSerilog((context, services, configuration) =>
    {
        var store = services.GetRequiredService<SerilogLogStore>();
        var logDir = context.Configuration["Serilog:JsonLogDirectory"];
        SerilogLogger.ConfigureAdminLogging(configuration, store, logDir);
    });

    builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddAuthorization(options => options.AddPermissionPolicies());

var corsOrigins = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()?.AllowedOrigins ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment() && corsOrigins.Length == 0)
        {
            policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
            return;
        }

        if (corsOrigins.Length > 0)
        {
            policy.WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("login", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 10;
        limiter.QueueLimit = 0;
    });
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync("Too many login attempts. Try again later.", token);
    };
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();
app.UseMiddleware<RequestMetricsMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();
app.UseRateLimiter();
app.UseMiddleware<SpnegoUserMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint для Redis и других зависимостей (доступен без аутентификации)
app.MapHealthChecks("/api/health").AllowAnonymous();

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

static void ValidateRequiredConfiguration(IConfiguration configuration, IHostEnvironment environment)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be set via configuration or environment variables.");

    var jwt = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
    if (string.IsNullOrWhiteSpace(jwt.Secret) || jwt.Secret.Length < 32)
        throw new InvalidOperationException("Jwt:Secret must be at least 32 characters. Set via environment variables or User Secrets.");

    var spnego = configuration.GetSection(SpnegoOptions.SectionName).Get<SpnegoOptions>() ?? new SpnegoOptions();
    if (spnego.Enabled && string.IsNullOrWhiteSpace(spnego.TrustedProxySecret))
        throw new InvalidOperationException("Spnego:TrustedProxySecret is required when Spnego:Enabled is true.");
}

static string NormalizeModelStateKey(string key)
{
    if (string.IsNullOrWhiteSpace(key))
        return "body";

    var parts = key.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (parts.Length == 0)
        return "body";

    if (parts.Length == 1)
        return ToCamelCase(parts[0]);

    return string.Join('.', parts.Select(ToCamelCase));
}

static string ToCamelCase(string value)
{
    if (string.IsNullOrEmpty(value))
        return value;

    if (char.IsLower(value[0]))
        return value;

    return char.ToLowerInvariant(value[0]) + value[1..];
}
