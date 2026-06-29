using ClosedXML.Excel;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Import;
using EngineeringPerformance.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace EngineeringPerformance.Tests;

/// <summary>
/// Replace-стратегия импорта оборудования на MS SQL (Testcontainers): upsert, skip ошибок без rollback.
/// </summary>
public sealed class DataImportReplaceStrategyTests : IAsyncLifetime
{
    private const string MsSqlPassword = "YourStrong@Passw0rd";

    private MsSqlContainer? _msSqlContainer;
    private IDbContextFactory<AppDbContext>? _factory;

    public async Task InitializeAsync()
    {
        _msSqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword(MsSqlPassword)
            .Build();

        await _msSqlContainer.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_msSqlContainer.GetConnectionString())
            .Options;

        _factory = new TestDbContextFactory(options);
        await using var ctx = _factory.CreateDbContext();
        await ctx.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_msSqlContainer is not null)
            await _msSqlContainer.DisposeAsync();
    }

    [Fact]
    public async Task Import_WithDuplicatesAndNewRows_ReplacesAndCommits()
    {
        _factory.Should().NotBeNull();
        var seeded = await SeedEquipmentRowsAsync(_factory!);

        var filePath = BuildEquipmentImportWorkbook(
            replaceRowId: seeded.ReplaceRowId,
            newRowFileId: seeded.UnknownFileRowId);
        try
        {
            var service = CreateImportService(_factory!);
            var result = await service.ImportProjectDataAsync(
                projectId: seeded.ProjectId,
                versionId: seeded.VersionId,
                DataImportType.Equipment,
                File.OpenRead(filePath),
                "equipment-replace.xlsx",
                description: "replace-test",
                contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                contentLength: new FileInfo(filePath).Length);

            result.Success.Should().BeTrue();
            result.RecordsImported.Should().Be(2, "обновлена существующая строка и вставлена новая");
            result.RowErrors.Should().NotBeNull();
            result.RowErrors!.Should().ContainSingle(e =>
                e.RowNumber == 4 && e.Message.Contains("Invalid", StringComparison.OrdinalIgnoreCase));

            await using var verify = await _factory!.CreateDbContextAsync();
            var rows = await verify.ProjectEquipments
                .Where(e => e.ProjectId == seeded.ProjectId && e.ProjectAnalysisVersionId == seeded.VersionId)
                .OrderBy(e => e.Id)
                .ToListAsync();

            rows.Should().HaveCount(4, "строки 1 и 3 сохранены, одна обновлена, добавлена новая");
            rows.Should().Contain(r => r.Id == seeded.KeepRowId1 && r.Quantity == 1m);
            rows.Should().Contain(r => r.Id == seeded.ReplaceRowId && r.Quantity == 99m, "REPLACE по Id из файла");
            rows.Should().Contain(r => r.Id == seeded.KeepRowId3 && r.Quantity == 3m);
            rows.Should().Contain(r => r.CatalogName == "New Pump" && r.Quantity == 5m);
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    private static ProjectExcelDataImportService CreateImportService(IDbContextFactory<AppDbContext> factory) =>
        ImportTestServiceFactory.Create(factory);

    private static async Task<SeedResult> SeedEquipmentRowsAsync(IDbContextFactory<AppDbContext> factory)
    {
        await using var ctx = await factory.CreateDbContextAsync();

        ctx.Projects.Add(new Project { Code = "PRJ", Name = "Test" });
        await ctx.SaveChangesAsync();

        var projectId = ctx.Projects.Select(p => p.Id).Single();
        ctx.ProjectAnalysisVersions.Add(new ProjectAnalysisVersion
        {
            ProjectId = projectId,
            VersionName = "v1",
            Direction = AnalysisDirection.Fact,
        });
        await ctx.SaveChangesAsync();

        var versionId = ctx.ProjectAnalysisVersions.Select(v => v.Id).Single();

        var eqA = new Equipment { Name = "Pump A", Coefficient = 1m };
        var eqB = new Equipment { Name = "Pump B", Coefficient = 1m };
        var eqC = new Equipment { Name = "Valve C", Coefficient = 1m };
        ctx.Equipments.AddRange(eqA, eqB, eqC);
        await ctx.SaveChangesAsync();

        ctx.ProjectEquipments.AddRange(
            new ProjectEquipment
            {
                ProjectId = projectId,
                ProjectAnalysisVersionId = versionId,
                EquipmentId = eqA.Id,
                Quantity = 1m,
                CatalogName = "Pump A",
                FacilityCode = "F1",
                Tag = "T1",
            },
            new ProjectEquipment
            {
                ProjectId = projectId,
                ProjectAnalysisVersionId = versionId,
                EquipmentId = eqB.Id,
                Quantity = 2m,
                CatalogName = "Pump B",
                FacilityCode = "F2",
                Tag = "T2",
            },
            new ProjectEquipment
            {
                ProjectId = projectId,
                ProjectAnalysisVersionId = versionId,
                EquipmentId = eqC.Id,
                Quantity = 3m,
                CatalogName = "Valve C",
                FacilityCode = "F3",
                Tag = "T3",
            });

        await ctx.SaveChangesAsync();

        var rows = await ctx.ProjectEquipments
            .Where(e => e.ProjectId == projectId && e.ProjectAnalysisVersionId == versionId)
            .OrderBy(e => e.Id)
            .ToListAsync();

        return new SeedResult(
            ProjectId: projectId,
            VersionId: versionId,
            KeepRowId1: rows[0].Id,
            ReplaceRowId: rows[1].Id,
            KeepRowId3: rows[2].Id,
            UnknownFileRowId: 50_000);
    }

    private sealed record SeedResult(
        int ProjectId,
        int VersionId,
        int KeepRowId1,
        int ReplaceRowId,
        int KeepRowId3,
        int UnknownFileRowId);

    private static string BuildEquipmentImportWorkbook(int replaceRowId, int newRowFileId)
    {
        var path = Path.Combine(Path.GetTempPath(), $"ep-equipment-{Guid.NewGuid():N}.xlsx");
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
            "",
            "Размерность",
        };

        for (var c = 0; c < headers.Length; c++)
            sheet.Cell(1, c + 1).Value = headers[c];

        sheet.Cell(2, 1).Value = replaceRowId;
        sheet.Cell(2, 7).Value = "Pump B";
        sheet.Cell(2, 14).Value = 99;

        sheet.Cell(3, 1).Value = newRowFileId;
        sheet.Cell(3, 7).Value = "New Pump";
        sheet.Cell(3, 14).Value = 5;

        sheet.Cell(4, 1).Value = newRowFileId;
        sheet.Cell(4, 14).Value = 1;

        workbook.SaveAs(path);
        return path;
    }
}
