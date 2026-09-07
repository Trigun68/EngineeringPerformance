using System.Threading.RateLimiting;
using EngineeringPerformance.Api.Auth;
using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Api.Configuration;
using EngineeringPerformance.Api.Middleware;
using EngineeringPerformance.Api.Validators;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Validators;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure;
using EngineeringPerformance.Infrastructure.Auth;
using EngineeringPerformance.Infrastructure.Logging;
using EngineeringPerformance.Infrastructure.Monitoring;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.StackExchangeRedis;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Prometheus;
using Serilog;
using StackExchange.Redis;
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
    builder.Services.AddScoped<AuthCookieHelper>();
    builder.Services.AddSingleton<ITrustedProxyValidator, TrustedProxyValidator>();

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
            problem.Extensions["correlationId"] =
                context.HttpContext.Items.TryGetValue(CorrelationIdMiddleware.CorrelationIdItemKey, out var cid)
                    ? cid?.ToString()
                    : null;
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
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Engineering Performance API",
            Version = "v1",
            Description = "HTTP API for SPA and external integration modules. Authenticate via POST /api/auth/login and send Authorization: Bearer.",
        });
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
        });
        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                Array.Empty<string>()
            },
        });
        options.TagActionsBy(api =>
        {
            var controller = api.ActionDescriptor.RouteValues.TryGetValue("controller", out var name)
                ? name
                : "Other";
            return [controller ?? "Other"];
        });
        options.CustomOperationIds(api =>
        {
            api.ActionDescriptor.RouteValues.TryGetValue("controller", out var controller);
            api.ActionDescriptor.RouteValues.TryGetValue("action", out var action);
            return $"{controller}_{action}";
        });
        // IFormFile + [FromForm] requires explicit binary mapping for Swashbuckle
        options.MapType<IFormFile>(() => new Microsoft.OpenApi.Models.OpenApiSchema
        {
            Type = "string",
            Format = "binary",
        });
        options.OperationFilter<EngineeringPerformance.Api.Swagger.FileUploadOperationFilter>();

        var apiXml = Path.Combine(AppContext.BaseDirectory, "EngineeringPerformance.Api.xml");
        if (File.Exists(apiXml))
            options.IncludeXmlComments(apiXml, includeControllerXmlComments: true);
        var appXml = Path.Combine(AppContext.BaseDirectory, "EngineeringPerformance.Application.xml");
        if (File.Exists(appXml))
            options.IncludeXmlComments(appXml);
    });

    // Health checks (конкретные чеки добавляются в AddInfrastructure)
    builder.Services.AddHealthChecks();

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddHostedService<ResourceMetricsSampler>();

    ConfigureDataProtection(builder);

    builder.Host.UseSerilog((context, services, configuration) =>
    {
        var store = services.GetRequiredService<SerilogLogStore>();
        var logDir = context.Configuration["Serilog:JsonLogDirectory"];
        SerilogLogger.ConfigureAdminLogging(configuration, store, logDir);
    });

    builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPermissionPolicies();
    // ADR-006: любой endpoint без явной политики требует аутентификации.
    // Анонимные точки (login, health) помечаются [AllowAnonymous] / .AllowAnonymous() явно.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

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
                .AllowCredentials()
                .WithExposedHeaders("Content-Disposition");
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
    options.AddFixedWindowLimiter("mfa", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 10;
        limiter.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("export", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 5;
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

var swaggerEnabled = app.Environment.IsDevelopment()
    || string.Equals(app.Configuration["Swagger:Enabled"], "true", StringComparison.OrdinalIgnoreCase);
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Engineering Performance API v1");
    });
}

app.UseForwardedHeaders();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestMetricsMiddleware>();
app.UseMiddleware<SqlProfilingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();
app.UseRateLimiter();
app.UseMiddleware<SpnegoUserMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint для Redis и других зависимостей (доступен без аутентификации)
app.MapHealthChecks("/api/health").AllowAnonymous();
app.MapHealthChecks("/health").AllowAnonymous();
// Prometheus scrape — только SuperAdmin (объект Administration); инфраструктурный сбор идёт через
// авторизованный сервисный аккаунт, анонимного /metrics нет.
app.MapMetrics("/metrics").RequireAuthorization($"Permission:{PermissionType.Administration}:{AccessLevel.Read}");

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

static void ConfigureDataProtection(WebApplicationBuilder builder)
{
    builder.Services
        .AddDataProtection()
        .SetApplicationName("EngineeringPerformance");

    var redisConnection = builder.Configuration.GetConnectionString("Redis");
    if (string.IsNullOrWhiteSpace(redisConnection))
        return;

    // Тот же IConnectionMultiplexer, что кэш и вкладка «Ресурсы» — без лишнего Redis-клиента.
    builder.Services.AddOptions<KeyManagementOptions>()
        .Configure<IConnectionMultiplexer>((options, mux) =>
        {
            options.XmlRepository = new RedisXmlRepository(
                () => mux.GetDatabase(),
                "EngineeringPerformance-DataProtection-Keys");
        });
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
