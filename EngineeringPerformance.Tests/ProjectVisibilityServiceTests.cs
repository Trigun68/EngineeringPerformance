using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Tests;

/// <summary>
/// V2-C: тесты на новую модель доступа через <see cref="ProjectPermission"/>.
/// Семантика — «нет записи → нет доступа», кроме Administrator.
/// </summary>
public sealed class ProjectVisibilityServiceTests
{
    private static IDbContextFactory<AppDbContext> CreateContextFactory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContextFactory(options);
    }

    private static UserDto CreateAdminDto(int roleId = 1) => new(1, "admin", "Администратор", roleId, nameof(UserRole.Administrator), null, true);
    private static UserDto CreateManagerDto(int roleId = 2) => new(1, "manager", "Менеджер", roleId, nameof(UserRole.ProjectManager), null, true);

    [Fact]
    public async Task Administrator_CanAccess_AnyProject()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();

        var adminRole = new Role { Name = nameof(UserRole.Administrator), Description = "Admin" };
        context.Roles.Add(adminRole);
        context.Projects.Add(new Project { Code = "PRJ-001", Name = "Test Project" });
        await context.SaveChangesAsync();

        var service = new ProjectVisibilityService(factory);
        var admin = CreateAdminDto(adminRole.Id);

        (await service.CanUserAccessProjectAsync(admin, 1)).Should().BeTrue();
    }

    [Fact]
    public async Task NonAdmin_CannotAccess_ProjectWithoutPermission()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();

        var pmRole = new Role { Name = nameof(UserRole.ProjectManager), Description = "PM" };
        context.Roles.Add(pmRole);
        context.Projects.Add(new Project { Code = "PRJ-001", Name = "Restricted" });
        await context.SaveChangesAsync();

        var service = new ProjectVisibilityService(factory);
        var manager = CreateManagerDto(pmRole.Id);

        (await service.CanUserAccessProjectAsync(manager, 1)).Should().BeFalse();
    }

    [Fact]
    public async Task NonAdmin_CanAccess_ProjectWithPermission()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();

        var pmRole = new Role { Name = nameof(UserRole.ProjectManager), Description = "PM" };
        context.Roles.Add(pmRole);
        context.Projects.Add(new Project { Code = "PRJ-001", Name = "Permitted" });
        await context.SaveChangesAsync();
        context.ProjectPermissions.Add(new ProjectPermission { ProjectId = 1, UserId = 1, Level = AccessLevel.Read });
        await context.SaveChangesAsync();

        var service = new ProjectVisibilityService(factory);
        var manager = CreateManagerDto(pmRole.Id);

        (await service.CanUserAccessProjectAsync(manager, 1)).Should().BeTrue();
    }

    [Fact]
    public async Task NonAdmin_CannotAccess_ArchivedProject_EvenWithPermission()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();

        var pmRole = new Role { Name = nameof(UserRole.ProjectManager), Description = "PM" };
        context.Roles.Add(pmRole);
        context.Projects.Add(new Project { Code = "PRJ-001", Name = "Archived", IsArchived = true });
        await context.SaveChangesAsync();
        context.ProjectPermissions.Add(new ProjectPermission { ProjectId = 1, UserId = 1, Level = AccessLevel.Read });
        await context.SaveChangesAsync();

        var service = new ProjectVisibilityService(factory);
        var manager = CreateManagerDto(pmRole.Id);

        (await service.CanUserAccessProjectAsync(manager, 1)).Should().BeFalse();
    }

    [Fact]
    public async Task GetAccessibleProjects_ReturnsAll_ForAdministrator()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();

        var adminRole = new Role { Name = nameof(UserRole.Administrator), Description = "Admin" };
        context.Roles.Add(adminRole);
        context.Projects.AddRange(
            new Project { Code = "PRJ-001", Name = "Project 1" },
            new Project { Code = "PRJ-002", Name = "Project 2" }
        );
        await context.SaveChangesAsync();

        var service = new ProjectVisibilityService(factory);
        var admin = CreateAdminDto(adminRole.Id);

        var projects = await service.GetAccessibleProjectsAsync(admin);
        projects.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAccessibleProjects_ReturnsOnlyPermitted_ForNonAdmin()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();

        var pmRole = new Role { Name = nameof(UserRole.ProjectManager), Description = "PM" };
        context.Roles.Add(pmRole);
        context.Projects.AddRange(
            new Project { Code = "PRJ-001", Name = "Project 1" },
            new Project { Code = "PRJ-002", Name = "Project 2" }
        );
        await context.SaveChangesAsync();
        context.ProjectPermissions.Add(new ProjectPermission { ProjectId = 1, UserId = 1, Level = AccessLevel.Read });
        await context.SaveChangesAsync();

        var service = new ProjectVisibilityService(factory);
        var manager = CreateManagerDto(pmRole.Id);

        var projects = await service.GetAccessibleProjectsAsync(manager);
        projects.Should().HaveCount(1);
        projects[0].Code.Should().Be("PRJ-001");
    }

    [Fact]
    public async Task AddUserToProject_CreatesPermission()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();

        var pmRole = new Role { Name = nameof(UserRole.ProjectManager), Description = "PM" };
        context.Roles.Add(pmRole);
        context.Projects.Add(new Project { Code = "PRJ-001", Name = "Test" });
        await context.SaveChangesAsync();

        var service = new ProjectVisibilityService(factory);
        await service.AddUserToProjectAsync(1, 1);

        (await service.CanUserAccessProjectAsync(CreateManagerDto(pmRole.Id), 1))
            .Should().BeTrue();
    }

    [Fact]
    public async Task RemoveUserFromProject_RemovesPermission()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();

        var pmRole = new Role { Name = nameof(UserRole.ProjectManager), Description = "PM" };
        context.Roles.Add(pmRole);
        context.Projects.Add(new Project { Code = "PRJ-001", Name = "Test" });
        await context.SaveChangesAsync();
        context.ProjectPermissions.Add(new ProjectPermission { ProjectId = 1, UserId = 1, Level = AccessLevel.Read });
        await context.SaveChangesAsync();

        var service = new ProjectVisibilityService(factory);
        await service.RemoveUserFromProjectAsync(1, 1);

        (await service.CanUserAccessProjectAsync(CreateManagerDto(pmRole.Id), 1))
            .Should().BeFalse();
    }

    [Fact]
    public async Task AddUserToProject_IsIdempotent()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();

        var pmRole = new Role { Name = nameof(UserRole.ProjectManager), Description = "PM" };
        context.Roles.Add(pmRole);
        context.Projects.Add(new Project { Code = "PRJ-001", Name = "Test" });
        await context.SaveChangesAsync();

        var service = new ProjectVisibilityService(factory);
        await service.AddUserToProjectAsync(1, 1);
        await service.AddUserToProjectAsync(1, 1);

        var permissions = await context.ProjectPermissions.CountAsync();
        permissions.Should().Be(1);
    }

    [Fact]
    public async Task AddOrUpdateUserPermission_CreatesWithSpecifiedLevel()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();
        context.Projects.Add(new Project { Id = 1, Code = "PRJ-001", Name = "Test" });
        await context.SaveChangesAsync();

        var service = new ProjectVisibilityService(factory);
        await service.AddOrUpdateUserPermissionAsync(projectId: 1, userId: 5, AccessLevel.Edit);

        var saved = await context.ProjectPermissions.AsNoTracking().SingleAsync();
        saved.UserId.Should().Be(5);
        saved.Level.Should().Be(AccessLevel.Edit);
    }

    [Fact]
    public async Task AddOrUpdateUserPermission_UpdatesLevel_WhenRecordExists()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();
        context.Projects.Add(new Project { Id = 1, Code = "PRJ-001", Name = "Test" });
        context.ProjectPermissions.Add(new ProjectPermission { ProjectId = 1, UserId = 5, Level = AccessLevel.Read });
        await context.SaveChangesAsync();

        var service = new ProjectVisibilityService(factory);
        await service.AddOrUpdateUserPermissionAsync(projectId: 1, userId: 5, AccessLevel.Edit);

        await using var verify = await factory.CreateDbContextAsync();
        var saved = await verify.ProjectPermissions.AsNoTracking().SingleAsync();
        saved.Level.Should().Be(AccessLevel.Edit, "повторный вызов с другим уровнем должен апдейтить, а не создавать дубликат");
        (await verify.ProjectPermissions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task AddOrUpdateUserPermission_IsNoOp_WhenLevelMatches()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();
        context.Projects.Add(new Project { Id = 1, Code = "PRJ-001", Name = "Test" });
        context.ProjectPermissions.Add(new ProjectPermission { ProjectId = 1, UserId = 5, Level = AccessLevel.Edit });
        await context.SaveChangesAsync();

        var service = new ProjectVisibilityService(factory);
        await service.AddOrUpdateUserPermissionAsync(projectId: 1, userId: 5, AccessLevel.Edit);

        await using var verify = await factory.CreateDbContextAsync();
        (await verify.ProjectPermissions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task GetProjectPermissions_ReturnsUsersWithLevelAndRole()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();

        var pmRole = new Role { Id = 2, Name = nameof(UserRole.ProjectManager), Description = "PM" };
        var observerRole = new Role { Id = 3, Name = nameof(UserRole.Observer), Description = "Obs" };
        context.Roles.AddRange(pmRole, observerRole);
        context.Users.AddRange(
            new User { Id = 5, Username = "manager", DisplayName = "Менеджер", PasswordHash = "x", RoleId = 2, IsActive = true, Name = "manager" },
            new User { Id = 6, Username = "observer", DisplayName = "Наблюдатель", PasswordHash = "x", RoleId = 3, IsActive = true, Name = "observer" }
        );
        context.Projects.Add(new Project { Id = 1, Code = "PRJ-001", Name = "Test" });
        context.ProjectPermissions.AddRange(
            new ProjectPermission { ProjectId = 1, UserId = 5, Level = AccessLevel.Edit },
            new ProjectPermission { ProjectId = 1, UserId = 6, Level = AccessLevel.Read }
        );
        await context.SaveChangesAsync();

        var service = new ProjectVisibilityService(factory);
        var permissions = await service.GetProjectPermissionsAsync(1);

        permissions.Should().HaveCount(2);
        permissions.Should().Contain(p => p.UserId == 5 && p.Level == AccessLevel.Edit && p.RoleName == "ProjectManager" && p.DisplayName == "Менеджер");
        permissions.Should().Contain(p => p.UserId == 6 && p.Level == AccessLevel.Read && p.RoleName == "Observer" && p.DisplayName == "Наблюдатель");
    }
}
