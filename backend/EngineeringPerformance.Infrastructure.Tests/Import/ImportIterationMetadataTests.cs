using ClosedXML.Excel;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Dictionaries;
using EngineeringPerformance.Application.Permissions;
using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Import;
using EngineeringPerformance.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EngineeringPerformance.Infrastructure.Tests.Import;

/// <summary>
/// ISS-009: происхождение файла (имя, SHA-256) и автор пишутся в итерацию импорта
/// внутри той же транзакции, что и данные, — без пост-коммитного дописывания.
/// </summary>
public sealed class ImportIterationMetadataTests
{
    private const int CurrentUserId = 42;

    [Fact]
    public async Task ImportProjectData_PersistsSourceMetadataWithIteration()
    {
        var factory = CreateFactory();
        var (projectId, versionId) = await SeedProjectAsync(factory);

        var filePath = BuildEquipmentWorkbook();
        try
        {
            var service = CreateService(factory, out var recalc);

            await using var upload = File.OpenRead(filePath);
            var result = await service.ImportProjectDataAsync(
                projectId,
                versionId,
                DataImportType.Equipment,
                upload,
                "Оборудование 2026.xlsx",
                description: "iss-009",
                contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                contentLength: new FileInfo(filePath).Length);

            result.Success.Should().BeTrue(result.ErrorMessage);

            await using var verify = await factory.CreateDbContextAsync();
            var iteration = await verify.DataImportIterations.AsNoTracking().SingleAsync();

            iteration.ImportedByUserId.Should().Be(CurrentUserId);
            iteration.SourceFileName.Should().Be(
                "Оборудование 2026.xlsx",
                "в итерации должно остаться имя загруженного файла, а не временный путь");
            iteration.SourceFileHash.Should().HaveLength(64, "SHA-256 в hex");

            recalc.Verify(
                x => x.RecalculateVersionAsync(projectId, versionId, It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    private static ProjectExcelDataImportService CreateService(
        IDbContextFactory<AppDbContext> factory,
        out Mock<IAnalysisVersionRecalculationOrchestrator> recalc)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UserId).Returns(CurrentUserId);

        var permissions = new Mock<IPermissionService>();
        permissions
            .Setup(x => x.CanEditAsync(CurrentUserId, PermissionType.ProjectDataUpload, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var visibility = new Mock<IProjectVisibilityService>();
        visibility
            .Setup(x => x.CanAccessProjectAsync(
                CurrentUserId,
                It.IsAny<int>(),
                AccessLevel.Edit,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        recalc = new Mock<IAnalysisVersionRecalculationOrchestrator>();
        recalc.Setup(x => x.RecalculateVersionAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        return new ProjectExcelDataImportService(
            factory,
            NullLogger<ProjectExcelDataImportService>.Instance,
            currentUser.Object,
            permissions.Object,
            visibility.Object,
            new Mock<IProjectAnalyticsCache>().Object,
            recalc.Object,
            Mock.Of<IUserActionLogger>(),
            new PhysicalVolumeExcelImportService(
                factory,
                NullLogger<PhysicalVolumeExcelImportService>.Instance),
            new EquipmentImportService(
                factory,
                NullLogger<EquipmentImportService>.Instance,
                new Mock<IDictionaryService>().Object),
            new LaborIntensityImportService(
                factory,
                NullLogger<LaborIntensityImportService>.Instance));
    }

    private static async Task<(int ProjectId, int VersionId)> SeedProjectAsync(IDbContextFactory<AppDbContext> factory)
    {
        await using var context = await factory.CreateDbContextAsync();

        var project = new Project { Code = "PRJ-009", Name = "ISS-009" };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var version = new ProjectAnalysisVersion
        {
            ProjectId = project.Id,
            VersionName = "v1",
            Direction = AnalysisDirection.Fact,
        };
        context.ProjectAnalysisVersions.Add(version);

        context.Equipments.Add(new Equipment { Name = "Насос", Coefficient = 1m });
        await context.SaveChangesAsync();

        return (project.Id, version.Id);
    }

    /// <summary>
    /// InMemory игнорирует транзакции; предупреждение подавляем, чтобы проверить
    /// именно состав метаданных итерации.
    /// </summary>
    private static IDbContextFactory<AppDbContext> CreateFactory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new InMemoryDbContextFactory(options);
    }

    private sealed class InMemoryDbContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
    }

    private static string BuildEquipmentWorkbook()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ep-iss009-{Guid.NewGuid():N}.xlsx");
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Equipment");

        var headers = new[]
        {
            "Id",
            "№ позиции в основном списке",
            "FBS (Code and Title of Facility)",
            "Комплект документов",
            "Диаграмма No / PFD No / P&ID No/ Datasheet No",
            "Наименование краткое",
            "Наименование по каталогу",
            "Тип/Марка оборудования",
            "ID по каталогу",
            "TAG",
            "ID обор. полный",
            "Стандарт на поставку",
            "ОЛ",
            "Всего, (ед)",
            "В работе, (ед)",
            "Базис оценки",
            string.Empty,
            "Размерность",
        };

        for (var column = 0; column < headers.Length; column++)
            sheet.Cell(1, column + 1).Value = headers[column];

        sheet.Cell(2, 1).Value = 1;
        sheet.Cell(2, 7).Value = "Насос";
        sheet.Cell(2, 14).Value = 3;

        workbook.SaveAs(path);
        return path;
    }
}
