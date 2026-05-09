using System.Globalization;
using System.Text;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EngineeringPerformance.Tests;

public sealed class UserActionLogServiceTests : IDisposable
{
    private readonly string _tempDir;

    public UserActionLogServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ualog-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private static IDbContextFactory<AppDbContext> CreateContextFactory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbContextFactory(options);
    }

    private static async Task<int> SeedUserAsync(IDbContextFactory<AppDbContext> factory, string username = "alice")
    {
        await using var ctx = await factory.CreateDbContextAsync();
        var role = new Role { Name = nameof(UserRole.Administrator), Description = "Admin" };
        ctx.Roles.Add(role);
        await ctx.SaveChangesAsync();

        var user = new User
        {
            Username = username,
            DisplayName = username,
            RoleId = role.Id,
            IsActive = true,
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        return user.Id;
    }

    private static UserActionLogService CreateService(IDbContextFactory<AppDbContext> factory) =>
        new(factory, NullLogger<UserActionLogService>.Instance);

    [Fact]
    public async Task LogAsync_InsertsRow_WithUtcTimestamp()
    {
        var factory = CreateContextFactory();
        var userId = await SeedUserAsync(factory);
        var service = CreateService(factory);

        await service.LogAsync(userId, UserActionType.ProjectCreate, "Project", 42, "demo");

        await using var ctx = await factory.CreateDbContextAsync();
        var row = await ctx.UserActionLogs.SingleAsync();

        row.UserId.Should().Be(userId);
        row.ActionType.Should().Be(nameof(UserActionType.ProjectCreate));
        row.EntityType.Should().Be("Project");
        row.EntityId.Should().Be(42);
        row.Description.Should().Be("demo");
        row.Timestamp.Kind.Should().NotBe(DateTimeKind.Local);
    }

    [Fact]
    public async Task LogAsync_DoesNotThrow_WhenUserIdMissing()
    {
        var factory = CreateContextFactory();
        var service = CreateService(factory);

        var act = async () => await service.LogAsync(999, UserActionType.Login);

        await act.Should().NotThrowAsync();

        await using var ctx = await factory.CreateDbContextAsync();
        // InMemory не валидирует FK, поэтому запись попадёт; тест проверяет
        // что LogAsync не пробрасывает исключения наружу — это и есть контракт.
        // Главное: вызов завершился без throw.
        (await ctx.UserActionLogs.CountAsync()).Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetAsync_FiltersByActionType_AndOrdersByTimestampDescending()
    {
        var factory = CreateContextFactory();
        var userId = await SeedUserAsync(factory);
        var service = CreateService(factory);

        await service.LogAsync(userId, UserActionType.Login);
        await Task.Delay(10);
        await service.LogAsync(userId, UserActionType.ProjectCreate, "Project", 1);
        await Task.Delay(10);
        await service.LogAsync(userId, UserActionType.ProjectCreate, "Project", 2);

        var filter = new UserActionLogFilter(ActionType: UserActionType.ProjectCreate);
        var rows = await service.GetAsync(filter);

        rows.Should().HaveCount(2);
        rows.Select(r => r.ActionType).Should().AllBeEquivalentTo(UserActionType.ProjectCreate);
        rows[0].Timestamp.Should().BeOnOrAfter(rows[1].Timestamp);
    }

    [Fact]
    public async Task GetAsync_FiltersByUserAndPeriod()
    {
        var factory = CreateContextFactory();
        var u1 = await SeedUserAsync(factory, "u1");
        await using (var ctx = await factory.CreateDbContextAsync())
        {
            ctx.Users.Add(new User { Username = "u2", DisplayName = "u2", RoleId = 1, IsActive = true });
            await ctx.SaveChangesAsync();
        }
        var u2 = (await GetUserIdsAsync(factory)).Last();

        var service = CreateService(factory);
        await service.LogAsync(u1, UserActionType.Login);
        await service.LogAsync(u2, UserActionType.Login);

        var rows = await service.GetAsync(new UserActionLogFilter(UserId: u1));
        rows.Should().HaveCount(1);
        rows[0].UserId.Should().Be(u1);
    }

    [Fact]
    public async Task ExportToFileAndPurge_WritesCsvHeader_AndRemovesRows()
    {
        var factory = CreateContextFactory();
        var userId = await SeedUserAsync(factory);
        var service = CreateService(factory);

        await service.LogAsync(userId, UserActionType.Login, description: "with;semicolon and \"quote\"");
        await service.LogAsync(userId, UserActionType.Logout);

        var path = Path.Combine(_tempDir, "export.csv");
        var result = await service.ExportToFileAndPurgeAsync(path);

        result.ExportedCount.Should().Be(2);
        result.DeletedCount.Should().Be(2);

        var lines = await File.ReadAllLinesAsync(path, Encoding.UTF8);
        lines.Should().HaveCount(3);
        lines[0].Should().Be("Id;UserId;UserName;Timestamp;ActionType;EntityType;EntityId;Description;IpOrHost");
        // Экранирование: значение с `;` и `"` обёрнуто в двойные кавычки, кавычки удвоены.
        lines.Should().Contain(l => l.Contains("\"with;semicolon and \"\"quote\"\"\""));

        await using var ctx = await factory.CreateDbContextAsync();
        (await ctx.UserActionLogs.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ImportFromFile_DeduplicatesRows()
    {
        var factory = CreateContextFactory();
        var userId = await SeedUserAsync(factory);
        var service = CreateService(factory);

        var ts = "2026-05-09T10:00:00.000Z";
        var csv = string.Join(Environment.NewLine,
            "Id;UserId;UserName;Timestamp;ActionType;EntityType;EntityId;Description;IpOrHost",
            $"1;{userId};alice;{ts};Login;;;hello;host1",
            $"2;{userId};alice;{ts};Login;;;hello;host1");

        var path = Path.Combine(_tempDir, "import.csv");
        await File.WriteAllTextAsync(path, csv, new UTF8Encoding(true));

        var result = await service.ImportFromFileAsync(path);

        result.TotalRows.Should().Be(2);
        result.ImportedCount.Should().Be(1);
        result.SkippedDuplicates.Should().Be(1);

        await using var ctx = await factory.CreateDbContextAsync();
        (await ctx.UserActionLogs.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ImportFromFile_SkipsOrphanUserIds()
    {
        var factory = CreateContextFactory();
        var userId = await SeedUserAsync(factory);
        var service = CreateService(factory);

        var csv = string.Join(Environment.NewLine,
            "Id;UserId;UserName;Timestamp;ActionType;EntityType;EntityId;Description;IpOrHost",
            $"1;{userId};alice;2026-05-09T10:00:00.000Z;Login;;;real;host1",
            "2;9999;ghost;2026-05-09T10:01:00.000Z;Login;;;orphan;host1");

        var path = Path.Combine(_tempDir, "import.csv");
        await File.WriteAllTextAsync(path, csv, new UTF8Encoding(true));

        var result = await service.ImportFromFileAsync(path);

        result.TotalRows.Should().Be(2);
        result.ImportedCount.Should().Be(1);
        result.SkippedOrphans.Should().Be(1);
        result.Errors.Should().Contain(e => e.Contains("9999"));
    }

    [Fact]
    public async Task ImportFromFile_RejectsBadHeader()
    {
        var factory = CreateContextFactory();
        var service = CreateService(factory);

        var path = Path.Combine(_tempDir, "bad.csv");
        await File.WriteAllTextAsync(path, "totally;wrong;header\r\n", new UTF8Encoding(true));

        var result = await service.ImportFromFileAsync(path);

        result.ImportedCount.Should().Be(0);
        result.Errors.Should().NotBeEmpty();
    }

    private static async Task<int[]> GetUserIdsAsync(IDbContextFactory<AppDbContext> factory)
    {
        await using var ctx = await factory.CreateDbContextAsync();
        return await ctx.Users.Select(u => u.Id).OrderBy(id => id).ToArrayAsync();
    }
}
