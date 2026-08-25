using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Export;
using EngineeringPerformance.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EngineeringPerformance.Infrastructure.Tests.Export;

public sealed class ExportFileNameServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<AppDbContext> SeedProjectWithVersionsAsync()
    {
        var context = CreateContext();
        context.Projects.Add(new Project { Id = 1, Code = "25-310", Name = "Реконструкция НС" });
        context.ProjectAnalysisVersions.AddRange(
            new ProjectAnalysisVersion
            {
                Id = 10,
                ProjectId = 1,
                VersionName = "Основная",
                Direction = AnalysisDirection.Plan,
                IsPrimary = true,
                CreatedAt = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            },
            new ProjectAnalysisVersion
            {
                Id = 11,
                ProjectId = 1,
                VersionName = "Факт 2025",
                Direction = AnalysisDirection.Fact,
                IsPrimary = true,
                CreatedAt = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc),
            });
        await context.SaveChangesAsync();
        return context;
    }

    [Fact]
    public async Task ForVersionAsync_BuildsCanonicalName()
    {
        await using var context = await SeedProjectWithVersionsAsync();
        var service = new ExportFileNameService(context);

        var name = await service.ForVersionAsync(1, 10, "прогноз", "xlsx", "итого");

        name.Should().Be("прогноз_итого_-_25-310_Реконструкция_НС_(Основная).xlsx");
    }

    [Fact]
    public async Task ForVersionAsync_UnknownVersion_Throws()
    {
        await using var context = await SeedProjectWithVersionsAsync();
        var service = new ExportFileNameService(context);

        var act = async () => await service.ForVersionAsync(1, 999, "прогноз", "xlsx");
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ForProjectAsync_UnknownProject_Throws()
    {
        await using var context = CreateContext();
        var service = new ExportFileNameService(context);

        var act = async () => await service.ForProjectAsync(42, "проекты", "xlsx");
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ForPerformanceReportAsync_NoIds_FallsBackToPrimaryPlanAndFact()
    {
        await using var context = await SeedProjectWithVersionsAsync();
        var service = new ExportFileNameService(context);

        var name = await service.ForPerformanceReportAsync(1, null, null);

        // Обе primary-версии (план + факт) попадают в блок версий через "_".
        name.Should().Be("отчет_-_25-310_Реконструкция_НС_(Основная_Факт_2025).pdf");
    }

    [Fact]
    public async Task ForPerformanceReportAsync_NoVersions_ReturnsProjectLevelName()
    {
        await using var context = CreateContext();
        context.Projects.Add(new Project { Id = 1, Code = "25-310", Name = "Реконструкция НС" });
        await context.SaveChangesAsync();
        var service = new ExportFileNameService(context);

        var name = await service.ForPerformanceReportAsync(1, null, null);

        name.Should().Be("отчет_-_25-310_Реконструкция_НС.pdf");
    }
}
