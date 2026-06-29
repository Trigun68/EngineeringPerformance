using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Tests;

/// <summary>
/// Регрессионные тесты для <see cref="ProjectVersionAnalysisService"/>:
/// проверяют корректную сборку отчёта «Анализ версии проекта» из трёх источников
/// (PerformanceService, ProjectPhysicalVolumes, ProjectLaborIntensities).
/// </summary>
public sealed class ProjectVersionAnalysisServiceTests
{
    private static IDbContextFactory<AppDbContext> CreateFactory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbContextFactory(options);
    }

    [Fact]
    public async Task GenerateReport_BuildsHeaderFromProjectAndVersion()
    {
        var factory = CreateFactory();
        await SeedAsync(factory);

        var service = new ProjectVersionAnalysisService(factory, new PerformanceService(factory));
        var report = await service.GenerateReportAsync(projectId: 1, versionId: 1);

        report.ProjectName.Should().Be("test");
        report.ProjectCode.Should().Be("PRJ");
        report.VersionName.Should().Be("v1");
    }

    [Fact]
    public async Task GenerateReport_DirectionsCollection_IncludesIicHierarchyOnly_ExcludesPlaceholder()
    {
        var factory = CreateFactory();
        await SeedAsync(factory);

        var service = new ProjectVersionAnalysisService(factory, new PerformanceService(factory));
        var report = await service.GenerateReportAsync(projectId: 1, versionId: 1);

        // PerformanceService добавляет в конец синтетическую строку «(пусто)» с Id < 0.
        // Сервис отчёта должен её отфильтровать (отчёт показывает только реальные направления IIC).
        report.Directions.Should().HaveCount(3);
        report.Directions.Select(d => d.Id).Should().BeEquivalentTo(new[] { 10, 11, 12 });
        report.Directions.Should().OnlyContain(d => d.Id >= 0);
    }

    [Fact]
    public async Task GenerateReport_PhysicalVolumes_GroupedByDirection_OnlyNonZeroAndClassifiable()
    {
        var factory = CreateFactory();
        await SeedAsync(factory);

        var service = new ProjectVersionAnalysisService(factory, new PerformanceService(factory));
        var report = await service.GenerateReportAsync(projectId: 1, versionId: 1);

        var leafA = report.Directions.Single(d => d.Id == 11);
        var leafB = report.Directions.Single(d => d.Id == 12);
        var root = report.Directions.Single(d => d.Id == 10);

        // leaf-A: одна позиция Quantity=100; нулевая позиция и позиция без ClassifierId — отфильтрованы.
        leafA.PhysicalVolumes.Should().ContainSingle();
        leafA.PhysicalVolumes[0].Code.Should().Be("PV-A");
        leafA.PhysicalVolumes[0].Quantity.Should().Be(100m);

        leafB.PhysicalVolumes.Should().ContainSingle();
        leafB.PhysicalVolumes[0].Code.Should().Be("PV-B");

        // У корня собственных физ. объёмов нет — в отчёте список пуст (агрегация по детям не делается на этом уровне).
        root.PhysicalVolumes.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateReport_LaborIntensities_PopulatedFromProjectRows_OrderedByDocumentationCode()
    {
        var factory = CreateFactory();
        await SeedAsync(factory);

        var service = new ProjectVersionAnalysisService(factory, new PerformanceService(factory));
        var report = await service.GenerateReportAsync(projectId: 1, versionId: 1);

        report.LaborIntensities.Should().HaveCount(2);
        report.LaborIntensities[0].DocumentationCode.Should().Be("DOC-001");
        report.LaborIntensities[1].DocumentationCode.Should().Be("DOC-002");
        report.LaborIntensities[0].PlannedHours.Should().Be(200m);
        report.LaborIntensities[0].ActualHours.Should().Be(200m);

        report.Summary.TotalFactLaborHours.Should().Be(350m);
        report.Summary.UnallocatedLaborHours.Should().BeNull();
    }

    [Fact]
    public async Task GenerateReport_Summary_IncludesUnallocatedLabor()
    {
        var factory = CreateFactory();
        await SeedAsync(factory);

        await using (var seed = factory.CreateDbContext())
        {
            seed.ProjectLaborIntensities.Add(new ProjectLaborIntensity
            {
                Id = 302,
                ProjectId = 1,
                ProjectAnalysisVersionId = 1,
                WorkDirectionId = null,
                ActualHours = 25m,
                PlannedHours = 25m,
                DocumentationCode = "DOC-UNALLOC"
            });
            await seed.SaveChangesAsync();
        }

        var service = new ProjectVersionAnalysisService(factory, new PerformanceService(factory));
        var report = await service.GenerateReportAsync(projectId: 1, versionId: 1);

        report.Summary.TotalFactLaborHours.Should().Be(375m);
        report.Summary.UnallocatedLaborHours.Should().Be(25m);
    }

    [Fact]
    public async Task GenerateReport_UnknownVersion_Throws()
    {
        var factory = CreateFactory();
        await SeedAsync(factory);

        var service = new ProjectVersionAnalysisService(factory, new PerformanceService(factory));

        var act = () => service.GenerateReportAsync(projectId: 1, versionId: 999);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static async Task SeedAsync(IDbContextFactory<AppDbContext> factory)
    {
        await using var seed = factory.CreateDbContext();

        seed.Projects.Add(new Project { Id = 1, Code = "PRJ", Name = "test" });

        seed.IicWorkDirections.Add(new IicWorkDirection { Id = 10, Name = "root", Code = "R" });
        seed.IicWorkDirections.Add(new IicWorkDirection { Id = 11, Name = "leaf-A", Code = "A", ParentId = 10 });
        seed.IicWorkDirections.Add(new IicWorkDirection { Id = 12, Name = "leaf-B", Code = "B", ParentId = 10 });

        seed.IicWorkDirectionNorms.Add(new IicWorkDirectionNorm
        {
            Id = 100,
            WorkDirectionId = 10,
            Performance = 2.5m,
            Estimate = 1.0m,
            StandardDeviation = 0.5m,
            EffectiveFrom = DateTime.UtcNow.AddDays(-30),
            EffectiveTo = null
        });

        seed.PhysicalVolumes.Add(new PhysicalVolume { Id = 50, Code = "PV-A", Name = "pv-a", ClassifierId = 11, Coefficient = 1m, VolumeTypeId = 1, IsAssessment = true });
        seed.PhysicalVolumes.Add(new PhysicalVolume { Id = 51, Code = "PV-B", Name = "pv-b", ClassifierId = 12, Coefficient = 1m, VolumeTypeId = 1, IsAssessment = true });
        // Дополнительные строки физ. объёмов: одна с нулевым кол-вом, одна без ClassifierId.
        // Сервис должен обе отфильтровать из секции отчёта.
        seed.PhysicalVolumes.Add(new PhysicalVolume { Id = 52, Code = "PV-Z", Name = "zero", ClassifierId = 11, Coefficient = 1m, VolumeTypeId = 1, IsAssessment = true });
        seed.PhysicalVolumes.Add(new PhysicalVolume { Id = 53, Code = "PV-N", Name = "no-classifier", ClassifierId = null, Coefficient = 1m, VolumeTypeId = 1, IsAssessment = true });

        seed.ProjectAnalysisVersions.Add(new ProjectAnalysisVersion
        {
            Id = 1,
            ProjectId = 1,
            VersionName = "v1",
            CreatedAt = DateTime.UtcNow,
            Direction = AnalysisDirection.Fact
        });
        await seed.SaveChangesAsync();

        seed.AnalysisVersionSnapshots.Add(new AnalysisVersionSnapshot
        {
            ProjectAnalysisVersionId = 1,
            WorkDirectionId = 10,
            NormId = 100
        });

        seed.ProjectPhysicalVolumes.Add(new ProjectPhysicalVolume { Id = 200, ProjectId = 1, ProjectAnalysisVersionId = 1, PhysicalVolumeId = 50, Quantity = 100m });
        seed.ProjectPhysicalVolumes.Add(new ProjectPhysicalVolume { Id = 201, ProjectId = 1, ProjectAnalysisVersionId = 1, PhysicalVolumeId = 51, Quantity = 50m });
        seed.ProjectPhysicalVolumes.Add(new ProjectPhysicalVolume { Id = 202, ProjectId = 1, ProjectAnalysisVersionId = 1, PhysicalVolumeId = 52, Quantity = 0m });
        seed.ProjectPhysicalVolumes.Add(new ProjectPhysicalVolume { Id = 203, ProjectId = 1, ProjectAnalysisVersionId = 1, PhysicalVolumeId = 53, Quantity = 25m });

        seed.ProjectLaborIntensities.Add(new ProjectLaborIntensity { Id = 301, ProjectId = 1, ProjectAnalysisVersionId = 1, WorkDirectionId = 12, ActualHours = 150m, PlannedHours = 150m, DocumentationCode = "DOC-002" });
        seed.ProjectLaborIntensities.Add(new ProjectLaborIntensity { Id = 300, ProjectId = 1, ProjectAnalysisVersionId = 1, WorkDirectionId = 11, ActualHours = 200m, PlannedHours = 200m, DocumentationCode = "DOC-001" });

        await seed.SaveChangesAsync();
    }
}
