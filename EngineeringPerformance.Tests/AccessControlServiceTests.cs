using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EngineeringPerformance.Tests;

public sealed class AccessControlServiceTests
{
    private static IDbContextFactory<AppDbContext> CreateContextFactory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContextFactory(options);
    }

    private static UserDto CreateAdminDto() => new(1, "admin", "Администратор", 1, nameof(UserRole.Administrator), null, true);
    private static UserDto CreateManagerDto(int roleId = 2) => new(1, "manager", "Менеджер", roleId, nameof(UserRole.ProjectManager), null, true);
    private static UserDto CreateObserverDto(int roleId = 3) => new(1, "observer", "Наблюдатель", roleId, nameof(UserRole.Observer), null, true);

    [Fact]
    public async Task Administrator_HasFullAccess_ToAllObjects()
    {
        var factory = CreateContextFactory();
        var service = new AccessControlService(factory, NullLogger<AccessControlService>.Instance);
        var admin = CreateAdminDto();

        service.HasAccess(admin, "Users", AccessLevel.Edit).Should().BeTrue();
        service.HasAccess(admin, "Projects", AccessLevel.Read).Should().BeTrue();
        service.HasAccess(admin, "Assessment", AccessLevel.Edit).Should().BeTrue();
    }

    [Fact]
    public async Task Manager_CanEdit_WhenAccessRightExists()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();

        var pmRole = new Role { Name = nameof(UserRole.ProjectManager), Description = "PM" };
        context.Roles.Add(pmRole);
        await context.SaveChangesAsync();

        context.AccessRights.Add(new AccessRight
        {
            RoleId = pmRole.Id,
            ObjectName = "Projects",
            AccessLevel = AccessLevel.Edit
        });
        await context.SaveChangesAsync();

        var service = new AccessControlService(factory, NullLogger<AccessControlService>.Instance);
        var manager = CreateManagerDto(pmRole.Id);

        service.CanEdit(manager, "Projects").Should().BeTrue();
        service.CanRead(manager, "Projects").Should().BeTrue();
    }

    [Fact]
    public async Task Observer_CanOnlyRead_WhenAccessRightIsRead()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();

        var obsRole = new Role { Name = nameof(UserRole.Observer), Description = "Observer" };
        context.Roles.Add(obsRole);
        await context.SaveChangesAsync();

        context.AccessRights.Add(new AccessRight
        {
            RoleId = obsRole.Id,
            ObjectName = "Projects",
            AccessLevel = AccessLevel.Read
        });
        await context.SaveChangesAsync();

        var service = new AccessControlService(factory, NullLogger<AccessControlService>.Instance);
        var observer = CreateObserverDto(obsRole.Id);

        service.CanRead(observer, "Projects").Should().BeTrue();
        service.CanEdit(observer, "Projects").Should().BeFalse();
    }

    [Fact]
    public async Task User_HasNoAccess_WhenNoRightExists()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();
        var pmRole = new Role { Name = nameof(UserRole.ProjectManager), Description = "PM" };
        context.Roles.Add(pmRole);
        await context.SaveChangesAsync();

        var service = new AccessControlService(factory, NullLogger<AccessControlService>.Instance);
        var manager = CreateManagerDto(pmRole.Id);

        service.CanRead(manager, "NonExistent").Should().BeFalse();
    }

    [Fact]
    public async Task GetAccessibleObjects_ReturnsAll_ForAdministrator()
    {
        var factory = CreateContextFactory();
        var service = new AccessControlService(factory, NullLogger<AccessControlService>.Instance);
        var admin = CreateAdminDto();

        var objects = service.GetAccessibleObjects(admin, AccessLevel.Read);
        objects.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAccessibleObjects_ReturnsFiltered_ForManager()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();

        var pmRole = new Role { Name = nameof(UserRole.ProjectManager), Description = "PM" };
        context.Roles.Add(pmRole);
        await context.SaveChangesAsync();

        context.AccessRights.AddRange(
            new AccessRight { RoleId = pmRole.Id, ObjectName = "Projects", AccessLevel = AccessLevel.Edit },
            new AccessRight { RoleId = pmRole.Id, ObjectName = "Users", AccessLevel = AccessLevel.Read }
        );
        await context.SaveChangesAsync();

        var service = new AccessControlService(factory, NullLogger<AccessControlService>.Instance);
        var manager = CreateManagerDto(pmRole.Id);

        var objects = service.GetAccessibleObjects(manager, AccessLevel.Edit);
        objects.Should().Contain("Projects");
        objects.Should().NotContain("Users");
    }
}
