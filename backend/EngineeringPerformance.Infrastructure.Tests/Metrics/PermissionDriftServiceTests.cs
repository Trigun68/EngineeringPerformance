using EngineeringPerformance.Application.Permissions;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Metrics;
using EngineeringPerformance.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Tests.Metrics;

public sealed class PermissionDriftServiceTests
{
    [Fact]
    public async Task GetPermissionDriftAsync_AdministratorWithoutDbRows_ReturnsNoDrift()
    {
        await using var context = CreateContext();
        SeedRoles(context);
        foreach (var obj in PermissionObjectCatalog.MatrixObjectNames)
        {
            context.AccessRights.AddRange(
                new AccessRight { RoleId = 2, ObjectName = obj, AccessLevel = AccessLevel.Edit },
                new AccessRight { RoleId = 3, ObjectName = obj, AccessLevel = AccessLevel.Read });
        }
        await context.SaveChangesAsync();

        var service = new PermissionDriftService(context);
        var drift = await service.GetPermissionDriftAsync();

        drift.HasDrift.Should().BeFalse();
        drift.UnknownObjects.Should().BeEmpty();
        drift.MissingObjectsByRole.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPermissionDriftAsync_MissingMatrixObjectForProjectManager_ReturnsDrift()
    {
        await using var context = CreateContext();
        SeedRoles(context);
        context.AccessRights.Add(new AccessRight
        {
            RoleId = 2,
            ObjectName = "Projects",
            AccessLevel = AccessLevel.Edit,
        });
        await context.SaveChangesAsync();

        var service = new PermissionDriftService(context);
        var drift = await service.GetPermissionDriftAsync();

        drift.HasDrift.Should().BeTrue();
        drift.MissingObjectsByRole.Should().ContainSingle(x => x.RoleName == "ProjectManager");
        drift.MissingObjectsByRole[0].MissingObjects.Should().NotContain(PermissionObjectCatalog.AdministrationObjectName);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static void SeedRoles(AppDbContext context)
    {
        context.Roles.AddRange(
            new Role { Id = 1, Name = PermissionObjectCatalog.AdministratorRoleName },
            new Role { Id = 2, Name = "ProjectManager" },
            new Role { Id = 3, Name = "Observer" });
    }
}
