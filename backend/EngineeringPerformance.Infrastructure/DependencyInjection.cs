using System.Text;
using EngineeringPerformance.Application.Admin;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Common.Interfaces;
using EngineeringPerformance.Application.Directories;
using EngineeringPerformance.Application.Dictionaries;
using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Application.Registers;
using EngineeringPerformance.Application.Users;
using EngineeringPerformance.Infrastructure.Dictionaries;
using EngineeringPerformance.Infrastructure.Directories;
using EngineeringPerformance.Infrastructure.Projects;
using EngineeringPerformance.Infrastructure.Registers;
using EngineeringPerformance.Infrastructure.Users;
using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Application.Import;
using EngineeringPerformance.Application.Monitoring;
using EngineeringPerformance.Application.Permissions;
using EngineeringPerformance.Application.Projects.Commands;
using EngineeringPerformance.Application.Projects.Queries;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Admin;
using EngineeringPerformance.Infrastructure.Analytics;
using EngineeringPerformance.Infrastructure.Audit;
using EngineeringPerformance.Infrastructure.Auth;
using EngineeringPerformance.Infrastructure.Export;
using EngineeringPerformance.Infrastructure.Logging;
using EngineeringPerformance.Infrastructure.Metrics;
using EngineeringPerformance.Infrastructure.Monitoring;
using EngineeringPerformance.Infrastructure.Import;
using EngineeringPerformance.Infrastructure.Persistence;
using EngineeringPerformance.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using Polly.Retry;
using StackExchange.Redis;
using Microsoft.IdentityModel.Tokens;

namespace EngineeringPerformance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));
        services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProjectRepository, ProjectRepository>();

        services.AddScoped<GetProjectsQueryHandler>();
        services.AddScoped<GetProjectByIdQueryHandler>();
        services.AddScoped<CreateProjectCommandHandler>();
        services.AddScoped<UpdateProjectCommandHandler>();
        services.AddScoped<ArchiveProjectCommandHandler>();

        services.AddScoped<IProjectExportService, ProjectExportService>();
        services.AddScoped<IProjectsCatalogExportService, ProjectsCatalogExportService>();
        services.AddSingleton<IRequestMetricsStore, RequestMetricsStore>();
        services.AddSingleton<SerilogLogStore>();
        services.AddScoped<ISystemMetricsService, SystemMetricsService>();
        services.AddScoped<ILogViewerService, LogViewerService>();
        services.AddScoped<Application.Monitoring.IHealthCheckService, Monitoring.HealthCheckService>();
        services.AddScoped<IAuditConfigService, AuditConfigService>();
        services.AddScoped<IProjectImportService, ProjectExcelDataImportService>();
        services.AddScoped<IDirectoryFacade, DirectoryFacade>();
        services.AddScoped<IIicNormService, IicWorkDirectionNormService>();
        services.AddSingleton<IPerformanceCalculationEngine, PerformanceCalculationEngine>();
        services.AddScoped<IProjectVisibilityService, ProjectVisibilityService>();
        services.AddScoped<IProjectVersionDeepCopyService, ProjectVersionDeepCopyService>();
        services.AddScoped<IProjectVersionService, ProjectVersionService>();
        services.AddScoped<IProjectDataService, ProjectDataService>();
        services.AddScoped<IPerformanceService, PerformanceService>();
        services.AddScoped<IProjectVersionAnalysisService, ProjectVersionAnalysisService>();
        services.AddScoped<IProjectForecastService, ProjectForecastService>();
        services.AddScoped<IForecastPlannedVolumePersistenceService, ForecastPlannedVolumePersistenceService>();
        services.AddScoped<ProjectVersionGridDataLoader>();
        services.AddScoped<ProjectsPageRawDataLoader>();
        services.AddScoped<IProjectsPageDataFetcher, ProjectsPageDataFetcher>();
        services.AddScoped<IProjectCompareService, ProjectCompareService>();
        services.AddScoped<IGroupAnalyticsService, GroupAnalyticsService>();
        services.AddScoped<IAnalyticsExportService, AnalyticsExportService>();
        services.AddScoped<IUserActionLogService, UserActionLogService>();
        services.AddScoped<IUserActionLogger, UserActionLogger>();
        services.AddScoped<IAccessRightsService, AccessRightsService>();
        services.AddScoped<IUserSettingsService, UserSettingsService>();
        services.AddScoped<IUserLookupService, UserLookupService>();
        services.AddScoped<IUserManagementService, UserManagementService>();

        services.AddScoped<IPermissionService>(sp =>
        {
            var context = sp.GetRequiredService<AppDbContext>();

            async Task<(int RoleId, string RoleName)?> GetUserRole(int userId, CancellationToken ct)
            {
                var user = await context.Users
                    .AsNoTracking()
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId, ct);

                return user is null ? null : (user.RoleId, user.Role.Name);
            }

            async Task<IReadOnlyDictionary<string, AccessLevel>> GetAccessRights(int roleId, CancellationToken ct)
            {
                var rights = await context.AccessRights
                    .AsNoTracking()
                    .Where(r => r.RoleId == roleId)
                    .ToListAsync(ct);

                return rights.ToDictionary(r => r.ObjectName, r => r.AccessLevel);
            }

            return new PermissionService(GetUserRole, GetAccessRights);
        });

        // Health check endpoint (конкретные чеки добавляются ниже)

        // Кэширование: Redis если настроен, иначе in-memory
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "EP_";
            });

            // Health check для Redis
            services.AddHealthChecks()
                .AddCheck<RedisHealthCheck>("redis", tags: ["cache", "infra"]);
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddScoped<IDictionaryService, CachedDictionaryService>();
        services.AddScoped<ICacheInvalidator, DictionaryCacheInvalidator>();

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? new JwtSettings();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
                };

                // JWT из httpOnly cookie (приоритет) или Authorization: Bearer
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (string.IsNullOrEmpty(context.Token)
                            && context.Request.Cookies.TryGetValue("ep_auth_token", out var cookieToken))
                        {
                            context.Token = cookieToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }
}
