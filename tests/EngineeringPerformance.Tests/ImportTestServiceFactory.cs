using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Permissions;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Dictionaries;
using EngineeringPerformance.Infrastructure.Import;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace EngineeringPerformance.Tests;

internal static class ImportTestServiceFactory
{
    public static ProjectExcelDataImportService Create(IDbContextFactory<AppDbContext> factory)
    {
        var permissionService = new PermissionService(
            (_, _) => Task.FromResult<(int RoleId, string RoleName)?>((1, nameof(UserRole.ProjectManager))),
            (_, _) => Task.FromResult<IReadOnlyDictionary<string, AccessLevel>>(
                new Dictionary<string, AccessLevel>
                {
                    [PermissionType.ProjectDataUpload.ToObjectName()] = AccessLevel.Edit,
                }));

        var dictionaryService = new CachedDictionaryService(factory, new MemoryCache(new MemoryCacheOptions()));

        return new ProjectExcelDataImportService(
            factory,
            NullLogger<ProjectExcelDataImportService>.Instance,
            new StubCurrentUserService(userId: 1),
            permissionService,
            new StubProjectVisibilityService(),
            dictionaryService,
            new StubAnalyticsCache(),
            new StubRecalcOrchestrator());
    }

    private sealed class StubAnalyticsCache : IProjectAnalyticsCache
    {
        public Task<T?> GetAsync<T>(int projectId, string key, CancellationToken cancellationToken = default)
            where T : class =>
            Task.FromResult<T?>(null);

        public Task SetAsync<T>(int projectId, string key, T value, CancellationToken cancellationToken = default)
            where T : class =>
            Task.CompletedTask;

        public Task InvalidateProjectAsync(int projectId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task InvalidateAllAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubRecalcOrchestrator : IAnalysisVersionRecalculationOrchestrator
    {
        public Task<int> RecalculateVersionAsync(int projectId, int versionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<int> AfterNormChangedAsync(
            int workDirectionId,
            DateTime effectiveFrom,
            DateTime? effectiveTo,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<int> AfterCatalogAssessmentOrCoefficientChangedAsync(
            string resource,
            int catalogId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<int> RecalculateAllVersionsAsync(
            bool includeArchived = true,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }
}
