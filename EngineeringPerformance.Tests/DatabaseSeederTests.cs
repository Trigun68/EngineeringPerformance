using EngineeringPerformance.Core;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Tests;

public sealed class DatabaseSeederTests
{
    private static IDbContextFactory<AppDbContext> CreateContextFactory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContextFactory(options);
    }

    [Fact]
    public async Task Seed_CreatesUsers()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();
        await DatabaseSeeder.SeedAsync(context);

        var users = await context.Users.Include(u => u.Role).ToListAsync();
        users.Should().HaveCount(3);
        users.Should().Contain(u => u.Username == "admin" && u.Role.Name == nameof(UserRole.Administrator));
        users.Should().Contain(u => u.Username == "manager" && u.Role.Name == nameof(UserRole.ProjectManager));
        users.Should().Contain(u => u.Username == "observer" && u.Role.Name == nameof(UserRole.Observer));
    }

    [Fact]
    public async Task Seed_CreatesRoles()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();
        await DatabaseSeeder.SeedAsync(context);

        var roles = await context.Roles.ToListAsync();
        roles.Should().HaveCount(3);
        roles.Should().Contain(r => r.Name == nameof(UserRole.Administrator));
        roles.Should().Contain(r => r.Name == nameof(UserRole.ProjectManager));
        roles.Should().Contain(r => r.Name == nameof(UserRole.Observer));
    }

    [Fact]
    public async Task Seed_CreatesUnitsOfMeasurement()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();
        await DatabaseSeeder.SeedAsync(context);

        var units = await context.UnitsOfMeasurement.ToListAsync();
        units.Should().HaveCount(12);
        units.Should().Contain(u => u.Name == "м³");
        units.Should().Contain(u => u.Name == "час");
    }

    [Fact]
    public async Task Seed_CreatesPhysicalVolumeTypes()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();
        await DatabaseSeeder.SeedAsync(context);

        var types = await context.PhysicalVolumeTypes.ToListAsync();
        types.Should().HaveCount(2);
        types.Should().Contain(t => t.Name == "БЕР");
        types.Should().Contain(t => t.Name == "КФО");
    }

    [Fact]
    public async Task Seed_CreatesIicWorkDirections()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();
        await DatabaseSeeder.SeedAsync(context);

        var directions = await context.IicWorkDirections.ToListAsync();
        directions.Should().HaveCount(4);
        directions.Should().Contain(d => d.Code == "IIC");
        directions.Should().Contain(d => d.Code == "IIC-01");
        directions.Should().Contain(d => d.Code == "IIC-02");
        directions.Should().Contain(d => d.Code == "IIC-03");
    }

    [Fact]
    public async Task Seed_CreatesIicWorkDirectionNorms()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();
        await DatabaseSeeder.SeedAsync(context);

        var norms = await context.IicWorkDirectionNorms.Include(n => n.Unit).ToListAsync();
        norms.Should().HaveCount(3);
        norms.Should().OnlyContain(n => n.UnitId.HasValue);
    }

    [Fact]
    public async Task Seed_CreatesProjects()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();
        await DatabaseSeeder.SeedAsync(context);

        var projects = await context.Projects.ToListAsync();
        projects.Should().HaveCount(3);
        projects.Should().Contain(p => p.Code == "PRJ-001");
        projects.Should().Contain(p => p.Code == "PRJ-002");
        projects.Should().Contain(p => p.Code == "PRJ-003");
    }

    [Fact]
    public async Task Seed_CreatesProjectAnalysisVersions()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();
        await DatabaseSeeder.SeedAsync(context);

        var versions = await context.ProjectAnalysisVersions.ToListAsync();
        versions.Should().HaveCount(5);
    }

    [Fact]
    public async Task Seed_CreatesAccessRights()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();
        await DatabaseSeeder.SeedAsync(context);

        var rights = await context.AccessRights.Include(r => r.Role).ToListAsync();
        rights.Should().HaveCount(AccessControlConstants.AllObjectNames.Length * 2);
        rights.Should().OnlyContain(r =>
            r.Role.Name == nameof(UserRole.ProjectManager) ||
            r.Role.Name == nameof(UserRole.Observer));

        var groupedByObject = rights.GroupBy(r => r.ObjectName).ToList();
        groupedByObject.Should().OnlyContain(g => g.Count() == 2);
        groupedByObject.Should().OnlyContain(g =>
            g.Any(r => r.Role.Name == nameof(UserRole.ProjectManager) && r.AccessLevel == AccessLevel.Edit) &&
            g.Any(r => r.Role.Name == nameof(UserRole.Observer) && r.AccessLevel == AccessLevel.Read));
    }

    [Fact]
    public async Task Seed_CreatesProjectPermissions_ForRestrictedProject()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();
        await DatabaseSeeder.SeedAsync(context);

        var permissions = await context.ProjectPermissions.ToListAsync();
        permissions.Should().HaveCount(2);
    }

    [Fact]
    public async Task Seed_IsIdempotent_DoesNotDuplicateOnSecondRun()
    {
        var factory = CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync();
        await DatabaseSeeder.SeedAsync(context);
        await DatabaseSeeder.SeedAsync(context);

        var users = await context.Users.CountAsync();
        users.Should().Be(3);

        var roles = await context.Roles.CountAsync();
        roles.Should().Be(3);

        var units = await context.UnitsOfMeasurement.CountAsync();
        units.Should().Be(12);
    }
}
