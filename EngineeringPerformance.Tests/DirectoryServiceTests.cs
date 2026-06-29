using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Tests;

public sealed class DirectoryServiceTests
{
    private static IDbContextFactory<AppDbContext> CreateContextFactory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContextFactory(options);
    }

    private static async Task SeedRoleAsync(AppDbContext context)
    {
        var role = new Role { Name = nameof(UserRole.ProjectManager), Description = "Test role" };
        context.Roles.Add(role);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task UserDirectoryService_CreateAndGetAll()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();
        await SeedRoleAsync(context);
        var roleId = context.Roles.First().Id;

        var service = new UserDirectoryService(factory);

        var user = await service.CreateAsync(new User
        {
            Username = "testuser",
            DisplayName = "Test User",
            RoleId = roleId,
            IsActive = true
        });

        var users = await service.GetAllAsync();
        users.Should().HaveCount(1);
        users[0].Username.Should().Be("testuser");
    }

    [Fact]
    public async Task UserDirectoryService_Update()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();
        await SeedRoleAsync(context);
        var roleId = context.Roles.First().Id;

        var service = new UserDirectoryService(factory);

        var user = await service.CreateAsync(new User
        {
            Username = "testuser",
            DisplayName = "Test User",
            RoleId = roleId,
            IsActive = true
        });

        user.DisplayName = "Updated Name";
        await service.UpdateAsync(user);

        var updated = await service.GetByIdAsync(user.Id);
        updated.Should().NotBeNull();
        updated!.DisplayName.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UserDirectoryService_Delete()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();
        await SeedRoleAsync(context);
        var roleId = context.Roles.First().Id;

        var service = new UserDirectoryService(factory);

        var user = await service.CreateAsync(new User
        {
            Username = "testuser",
            DisplayName = "Test User",
            RoleId = roleId,
            IsActive = true
        });

        await service.DeleteAsync(user.Id);

        var users = await service.GetAllAsync();
        users.Should().BeEmpty();
    }

    [Fact]
    public async Task UserDirectoryService_ArchiveAndUnarchive()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();
        await SeedRoleAsync(context);
        var roleId = context.Roles.First().Id;

        var service = new UserDirectoryService(factory);

        var user = await service.CreateAsync(new User
        {
            Username = "testuser",
            DisplayName = "Test User",
            RoleId = roleId,
            IsActive = true
        });

        await service.ArchiveAsync(user.Id);

        var activeUsers = await service.GetAllAsync();
        activeUsers.Should().BeEmpty();

        var allUsers = await service.GetAllAsync(includeArchived: true);
        allUsers.Should().HaveCount(1);

        await service.UnarchiveAsync(user.Id);

        var restoredUsers = await service.GetAllAsync();
        restoredUsers.Should().HaveCount(1);
    }

    [Fact]
    public async Task ProjectDirectoryService_CreateAndGetAll()
    {
        var factory = CreateContextFactory();
        var service = new ProjectDirectoryService(factory);

        var project = await service.CreateAsync(new Project
        {
            Code = "PRJ-TEST",
            Name = "Test Project",
        });

        var projects = await service.GetAllAsync();
        projects.Should().HaveCount(1);
        projects[0].Code.Should().Be("PRJ-TEST");
    }
}
