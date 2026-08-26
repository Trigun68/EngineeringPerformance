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
using EngineeringPerformance.Application.Settings;
using EngineeringPerformance.Application.Users;
using EngineeringPerformance.Infrastructure.Dictionaries;
using EngineeringPerformance.Infrastructure.Directories;
using EngineeringPerformance.Infrastructure.Projects;
using EngineeringPerformance.Infrastructure.Registers;
using EngineeringPerformance.Infrastructure.Users;
using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Application.Import;
using EngineeringPerformance.Application.Norms;
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
using EngineeringPerformance.Application.Monitoring;
using EngineeringPerformance.Infrastructure.Metrics;
using EngineeringPerformance.Infrastructure.Monitoring;
using EngineeringPerformance.Infrastructure.LaborIntensityServices;
using EngineeringPerformance.Infrastructure.Import;
using EngineeringPerformance.Infrastructure.Persistence;
using EngineeringPerformance.Infrastructure.Repositories;
using EngineeringPerformance.Infrastructure.Settings;
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
        services.Configure<MfaOptions>(configuration.GetSection(MfaOptions.SectionName));
        services.Configure<RateLimitOptions>(configuration.GetSection(RateLimitOptions.SectionName));
        services.Configure<AuthAuditOptions>(configuration.GetSection(AuthAuditOptions.SectionName));
        services.Configure<LdapOptions>(configuration.GetSection(LdapOptions.SectionName));
        services.Configure<AdminOptions>(configuration.GetSection(AdminOptions.SectionName));

        if (configuration.GetValue<bool>($"{LdapOptions.SectionName}:Enabled"))
            services.AddScoped<ILdapService, LdapService>();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.Configure<Diagnostics.SqlProfilingOptions>(
            configuration.GetSection(Diagnostics.SqlProfilingOptions.SectionName));
        services.Configure<Application.Analytics.FactNorm.AnalyticsOptions>(
            configuration.GetSection(Application.Analytics.FactNorm.AnalyticsOptions.SectionName));
        services.Configure<Application.Analytics.NormRegistry.NormHealthOptions>(
            configuration.GetSection(Application.Analytics.NormRegistry.NormHealthOptions.SectionName));
        services.Configure<AnalyticsTrendsOptions>(
            configuration.GetSection(AnalyticsTrendsOptions.SectionName));
        var analyticsOptions = configuration
            .GetSection(Application.Analytics.FactNorm.AnalyticsOptions.SectionName)
            .Get<Application.Analytics.FactNorm.AnalyticsOptions>()
            ?? new Application.Analytics.FactNorm.AnalyticsOptions();
        analyticsOptions.Validate();
        services.AddSingleton<Diagnostics.SqlQueryCommandInterceptor>();
        ConfigureSqlLanes(services, connectionString);
        services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IAccountLockoutService, AccountLockoutService>();
        services.AddScoped<IMfaService, MfaService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuthAuditService, AuthAuditService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProjectRepository, ProjectRepository>();

        services.AddScoped<GetProjectsQueryHandler>();
        services.AddScoped<GetProjectByIdQueryHandler>();
        services.AddScoped<CreateProjectCommandHandler>();
        services.AddScoped<UpdateProjectCommandHandler>();
        services.AddScoped<ArchiveProjectCommandHandler>();
        services.AddScoped<UnarchiveProjectCommandHandler>();
        services.AddScoped<DeleteProjectCommandHandler>();

        services.AddScoped<IProjectExportService, ProjectExportService>();
        services.AddScoped<IDirectoryExportService, DirectoryExportService>();
        services.AddScoped<IExportFileNameService, ExportFileNameService>();
        services.AddScoped<IForecastDetailSummaryTableContext, ForecastDetailSummaryTableContext>();
        services.AddScoped<IProjectsCatalogExportService, ProjectsCatalogExportService>();
        services.AddSingleton<IRequestMetricsStore, RequestMetricsStore>();
        services.AddSingleton<SerilogLogStore>();
        services.AddScoped<ISystemMetricsService, SystemMetricsService>();
        services.AddScoped<IPermissionDriftService, PermissionDriftService>();
        services.AddScoped<ILogViewerService, LogViewerService>();
        services.AddScoped<Application.Monitoring.IHealthCheckService, Monitoring.HealthCheckService>();
        services.AddScoped<IAuditConfigService, AuditConfigService>();
        services.AddScoped<ISqlProfilingPersistence, Diagnostics.SqlProfilingPersistence>();
        services.AddScoped<ISqlProfilingReportService, Diagnostics.SqlProfilingReportService>();
        services.AddScoped<IProjectImportService, ProjectExcelDataImportService>();
        services.AddScoped<IDirectoryFacade, DirectoryFacade>();
        services.AddScoped<IIicNormService, IicWorkDirectionNormService>();
        services.AddScoped<ILaborIntensityRecalculationService, LaborIntensityRecalculationService>();
        services.AddScoped<IAnalysisVersionRecalculationOrchestrator, AnalysisVersionRecalculationOrchestrator>();
        services.AddScoped<IAggregatedLaborIntensitiesService, AggregatedLaborIntensitiesService>();
        services.AddSingleton<INormCalculator, NormCalculator>();
        services.AddSingleton<IDeviationCalculator, DeviationCalculator>();
        services.AddSingleton<INormPreviewService, NormPreviewService>();
        services.AddSingleton<IPerformanceCalculationEngine, PerformanceCalculationEngine>();
        services.AddScoped<IProjectVisibilityService, ProjectVisibilityService>();
        services.AddScoped<IProjectCharacteristicSelectionService, ProjectCharacteristicSelectionService>();
        services.AddScoped<IProjectVersionDeepCopyService, ProjectVersionDeepCopyService>();
        services.AddScoped<IProjectVersionPlanningParameterService, ProjectVersionPlanningParameterService>();
        services.AddScoped<IProjectVersionService, ProjectVersionService>();
        services.AddScoped<IProjectVersionReadinessService, ProjectVersionReadinessService>();
        services.AddScoped<IProjectDataService, ProjectDataService>();
        services.AddScoped<IPerformanceService, PerformanceService>();
        services.AddScoped<IProjectVersionAnalysisService, ProjectVersionAnalysisService>();
        services.AddScoped<IPerformanceReportService, PerformanceReportService>();
        services.AddScoped<IProjectReportSummaryService, ProjectReportSummaryService>();
        services.AddScoped<IProjectForecastService, ProjectForecastService>();
        services.AddScoped<IForecastPlannedVolumePersistenceService, ForecastPlannedVolumePersistenceService>();
        services.AddScoped<ProjectVersionGridDataLoader>();
        services.AddSingleton<AnalyticsSqlGate>();
        services.AddSingleton<IPageDataBuildCoalescer, PageDataBuildCoalescer>();
        services.AddScoped<ProjectsPageRawDataLoader>();
        services.AddScoped<IProjectsPageRawDataLoader>(sp =>
            sp.GetRequiredService<ProjectsPageRawDataLoader>());
        services.AddScoped<IProjectsPageDataFetcher, ProjectsPageDataFetcher>();
        services.AddScoped<IProjectCompareService, ProjectCompareService>();
        services.AddScoped<IFactAssessmentPipeline, FactAssessmentPipeline>();
        services.AddSingleton<Application.Analytics.FactNorm.IFactNormAnalysisEngine,
            Application.Analytics.FactNorm.FactNormAnalysisEngine>();
        services.AddScoped<Application.Analytics.FactNorm.IFactNormCacheGeneration, FactNormCacheGeneration>();
        services.AddScoped<Application.Analytics.IProjectAnalyticsCache, ProjectAnalyticsCache>();
        services.AddScoped<Application.Analytics.FactNorm.IFactNormAnalysisService, FactNormAnalysisService>();
        services.AddScoped<Application.Analytics.NormRegistry.INormUsageStatsService,
            Analytics.NormRegistry.NormUsageStatsService>();
        services.AddScoped<Analytics.NormRegistry.NormRegistryService>();
        services.AddScoped<Application.Analytics.NormRegistry.INormRegistryService>(sp =>
            sp.GetRequiredService<Analytics.NormRegistry.NormRegistryService>());
        services.AddScoped<Application.Analytics.NormRegistry.INormHealthCheckService>(sp =>
            sp.GetRequiredService<Analytics.NormRegistry.NormRegistryService>());
        services.AddScoped<ITrendAnalyticsService, TrendAnalyticsService>();
        services.AddScoped<IZTrendEngine, ZTrendEngine>();
        services.AddScoped<IZTrendAnalyticsService, ZTrendAnalyticsService>();
        services.AddScoped<IProductivityTrendAnalyticsService, ProductivityTrendAnalyticsService>();
        services.AddScoped<IGroupAnalyticsService, GroupAnalyticsService>();
        services.AddScoped<IAnalyticsExportService, AnalyticsExportService>();
        services.AddScoped<IChartImageRenderer, ScottPlotChartRenderer>();
        services.AddScoped<IVisualReportService, VisualReportService>();
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
        services.AddScoped<ISystemSettingsService, SystemSettingsService>();

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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ClockSkew = TimeSpan.FromSeconds(jwtSettings.ClockSkewSeconds)
                };

                // JWT из httpOnly cookie (приоритет) или Authorization: Bearer
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (string.IsNullOrEmpty(context.Token)
                            && context.Request.Cookies.TryGetValue(jwtSettings.AccessTokenCookieName, out var cookieToken))
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

    private static void ConfigureSqlLanes(IServiceCollection services, string? connectionString)
    {
        services.AddDbContextFactory<AppDbContext>((sp, options) =>
        {
            var interactive = string.IsNullOrWhiteSpace(connectionString)
                ? connectionString
                : SqlConnectionStringLane.ForInteractive(connectionString);
            options.UseSqlServer(interactive, sql =>
            {
                sql.CommandTimeout(30);
                sql.ExecutionStrategy(dependencies =>
                    new SqlLaneRetryingExecutionStrategy(dependencies, 3, TimeSpan.FromSeconds(2)));
            });
            options.AddInterceptors(sp.GetRequiredService<Diagnostics.SqlQueryCommandInterceptor>());
        });

        services.AddSingleton<IAnalyticsDbContextFactory>(sp =>
        {
            var interceptor = sp.GetRequiredService<Diagnostics.SqlQueryCommandInterceptor>();
            var builder = new DbContextOptionsBuilder<AppDbContext>();
            var analytics = string.IsNullOrWhiteSpace(connectionString)
                ? connectionString
                : SqlConnectionStringLane.ForAnalytics(connectionString);
            builder.UseSqlServer(analytics, sql =>
            {
                sql.CommandTimeout(60);
                sql.ExecutionStrategy(dependencies =>
                    new SqlLaneRetryingExecutionStrategy(dependencies, 3, TimeSpan.FromSeconds(2)));
            });
            builder.AddInterceptors(interceptor);
            return new AnalyticsDbContextFactory(builder.Options);
        });
    }
}
