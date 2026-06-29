using ClosedXML.Excel;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace EngineeringPerformance.Tests;

/// <summary>
/// Валидация ссылок на справочники при импорте оборудования (UnitId, DirectionId, NormType).
/// </summary>
public sealed class ImportDictionaryValidationTests : IAsyncLifetime
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
    public async Task Import_WithInvalidDictionaryReference_ReturnsRowError()
    {
        _factory.Should().NotBeNull();
        var seeded = await SeedDictionariesAndProjectAsync(_factory!);

        var filePath = BuildDictionaryValidationWorkbook(
            validUnitId: seeded.UnitIds[0],
            invalidUnitId: 999,
            invalidDirectionId: 888);
        try
        {
            var service = ImportTestServiceFactory.Create(_factory!);
            var result = await service.ImportProjectDataAsync(
                projectId: seeded.ProjectId,
                versionId: seeded.VersionId,
                DataImportType.Equipment,
                File.OpenRead(filePath),
                "dictionary-invalid.xlsx",
                description: "dictionary-validation-invalid",
                contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                contentLength: new FileInfo(filePath).Length);

            result.Success.Should().BeTrue("транзакция коммитится несмотря на ошибки строк");
            result.RecordsImported.Should().Be(1, "только строка с валидным UnitId");
            result.RowErrors.Should().NotBeNull();
            result.RowErrors!.Should().HaveCount(2);
            result.RowErrors.Should().Contain(e =>
                e.Message.Contains("Unit not found", StringComparison.OrdinalIgnoreCase));
            result.RowErrors.Should().Contain(e =>
                e.Message.Contains("Direction not found", StringComparison.OrdinalIgnoreCase));

            await using var verify = await _factory!.CreateDbContextAsync();
            var rows = await verify.ProjectEquipments
                .Where(e => e.ProjectId == seeded.ProjectId && e.ProjectAnalysisVersionId == seeded.VersionId)
                .ToListAsync();

            rows.Should().ContainSingle();
            rows[0].CatalogName.Should().Be("Eq Valid");
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Fact]
    public async Task Import_WithValidDictionaryReferences_AllRowsImported()
    {
        _factory.Should().NotBeNull();
        var seeded = await SeedDictionariesAndProjectAsync(_factory!);

        var filePath = BuildValidDictionaryWorkbook(
            unitId: seeded.UnitIds[0],
            directionId: seeded.DirectionIds[0],
            normType: seeded.NormTypeCodes[0]);
        try
        {
            var service = ImportTestServiceFactory.Create(_factory!);
            var result = await service.ImportProjectDataAsync(
                projectId: seeded.ProjectId,
                versionId: seeded.VersionId,
                DataImportType.Equipment,
                File.OpenRead(filePath),
                "dictionary-valid.xlsx",
                description: "dictionary-validation-valid",
                contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                contentLength: new FileInfo(filePath).Length);

            result.Success.Should().BeTrue();
            result.RecordsImported.Should().Be(3);
            result.RowErrors.Should().NotBeNull();
            result.RowErrors!.Should().BeEmpty();

            await using var verify = await _factory!.CreateDbContextAsync();
            var rows = await verify.ProjectEquipments
                .Where(e => e.ProjectId == seeded.ProjectId && e.ProjectAnalysisVersionId == seeded.VersionId)
                .ToListAsync();

            rows.Should().HaveCount(3);
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    private static async Task<DictionarySeedResult> SeedDictionariesAndProjectAsync(
        IDbContextFactory<AppDbContext> factory)
    {
        await using var ctx = await factory.CreateDbContextAsync();

        ctx.UnitsOfMeasurement.AddRange(
            new UnitOfMeasurement { Name = "шт" },
            new UnitOfMeasurement { Name = "м" },
            new UnitOfMeasurement { Name = "кг" });
        ctx.IicWorkDirections.AddRange(
            new IicWorkDirection { Name = "Direction A", Code = "DA" },
            new IicWorkDirection { Name = "Direction B", Code = "DB" });
        ctx.PhysicalVolumeTypes.AddRange(
            new PhysicalVolumeType { Name = "Norm Type 1", Code = "NT1" },
            new PhysicalVolumeType { Name = "Norm Type 2", Code = "NT2" },
            new PhysicalVolumeType { Name = "Norm Type 3", Code = "NT3" });

        ctx.Projects.Add(new Project { Code = "PRJ-DICT", Name = "Dictionary Test" });
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
        var unitIds = await ctx.UnitsOfMeasurement.OrderBy(u => u.Id).Select(u => u.Id).ToListAsync();
        var directionIds = await ctx.IicWorkDirections.OrderBy(d => d.Id).Select(d => d.Id).ToListAsync();
        var normTypeCodes = await ctx.PhysicalVolumeTypes.OrderBy(t => t.Id).Select(t => t.Code).ToListAsync();

        return new DictionarySeedResult(projectId, versionId, unitIds, directionIds, normTypeCodes);
    }

    private sealed record DictionarySeedResult(
        int ProjectId,
        int VersionId,
        IReadOnlyList<int> UnitIds,
        IReadOnlyList<int> DirectionIds,
        IReadOnlyList<string> NormTypeCodes);

    private static string BuildDictionaryValidationWorkbook(
        int validUnitId,
        int invalidUnitId,
        int invalidDirectionId)
    {
        var path = Path.Combine(Path.GetTempPath(), $"ep-dict-invalid-{Guid.NewGuid():N}.xlsx");
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Equipment");

        WriteEquipmentHeaders(sheet, includeDictionaryColumns: true);

        WriteEquipmentDataRow(sheet, 2, "Eq Valid", quantity: 1, unitId: validUnitId);
        WriteEquipmentDataRow(sheet, 3, "Eq Bad Unit", quantity: 2, unitId: invalidUnitId);
        WriteEquipmentDataRow(sheet, 4, "Eq Bad Direction", quantity: 3, unitId: validUnitId, directionId: invalidDirectionId);

        workbook.SaveAs(path);
        return path;
    }

    private static string BuildValidDictionaryWorkbook(int unitId, int directionId, string normType)
    {
        var path = Path.Combine(Path.GetTempPath(), $"ep-dict-valid-{Guid.NewGuid():N}.xlsx");
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Equipment");

        WriteEquipmentHeaders(sheet, includeDictionaryColumns: true);

        WriteEquipmentDataRow(sheet, 2, "Eq 1", quantity: 1, unitId: unitId, directionId: directionId, normType: normType);
        WriteEquipmentDataRow(sheet, 3, "Eq 2", quantity: 2, unitId: unitId, directionId: directionId, normType: normType);
        WriteEquipmentDataRow(sheet, 4, "Eq 3", quantity: 3, unitId: unitId, directionId: directionId, normType: normType);

        workbook.SaveAs(path);
        return path;
    }

    private static void WriteEquipmentHeaders(IXLWorksheet sheet, bool includeDictionaryColumns)
    {
        var headers = new List<string>
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

        if (includeDictionaryColumns)
        {
            headers.Add("UnitId");
            headers.Add("DirectionId");
            headers.Add("NormType");
        }

        for (var c = 0; c < headers.Count; c++)
            sheet.Cell(1, c + 1).Value = headers[c];
    }

    private static void WriteEquipmentDataRow(
        IXLWorksheet sheet,
        int row,
        string catalogName,
        decimal quantity,
        int? unitId = null,
        int? directionId = null,
        string? normType = null)
    {
        sheet.Cell(row, 7).Value = catalogName;
        sheet.Cell(row, 14).Value = quantity;

        if (unitId.HasValue)
            sheet.Cell(row, 19).Value = unitId.Value;
        if (directionId.HasValue)
            sheet.Cell(row, 20).Value = directionId.Value;
        if (!string.IsNullOrWhiteSpace(normType))
            sheet.Cell(row, 21).Value = normType;
    }
}
