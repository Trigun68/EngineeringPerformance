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
            dictionaryService);
    }
}
