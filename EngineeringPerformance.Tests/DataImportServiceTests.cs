using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EngineeringPerformance.Tests;

/// <summary>
/// Базовое покрытие <see cref="DataImportService"/>: путь без файла, повторяющиеся импорты,
/// доступ к итерациям. Полная проверка парсинга Excel требует фикстуры файлов.
/// </summary>
public sealed class DataImportServiceTests
{
    private static IDbContextFactory<AppDbContext> CreateFactory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContextFactory(options);
    }

    [Fact]
    public async Task ImportFromFile_FileNotFound_ReturnsFailureResult()
    {
        var factory = CreateFactory();
        var service = new DataImportService(factory, NullLogger<DataImportService>.Instance);

        var result = await service.ImportFromFileAsync(
            projectId: 1,
            versionId: 1,
            dataType: DataImportType.PhysicalVolume,
            filePath: Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.xlsx"),
            description: "test");

        result.Success.Should().BeFalse();
        result.RecordsImported.Should().Be(0);
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetImportIterations_NoData_ReturnsEmpty()
    {
        var factory = CreateFactory();
        var service = new DataImportService(factory, NullLogger<DataImportService>.Instance);

        var iterations = await service.GetImportIterationsAsync(1, 1, DataImportType.PhysicalVolume);

        iterations.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteIteration_NoData_OnInMemoryProvider_TransactionsNotSupported()
    {
        var factory = CreateFactory();
        var service = new DataImportService(factory, NullLogger<DataImportService>.Instance);

        // DataImportService использует транзакции; провайдер InMemory их не поддерживает.
        // Тест документирует контракт: для интеграционных проверок нужна реальная БД.
        var act = () => service.DeleteIterationAsync(1, 1, DataImportType.PhysicalVolume, DateTime.UtcNow);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// ISS-006: при импорте трудоёмкости несколько строк с одинаковыми
    /// (DocumentationCode, WorkDirectionId) должны суммироваться по плановым
    /// и фактическим часам, а не затирать друг друга.
    /// </summary>
    [Fact]
    public void MergeLaborRowsByNaturalKey_DuplicateKey_SumsPlannedAndActualHours()
    {
        var rows = new[]
        {
            new ProjectLaborIntensity
            {
                DocumentationCode = "VS_04|3U0000-ОП-АТХ1",
                WorkDirectionId = 7,
                PlannedHours = 12.5m,
                ActualHours = 3m,
            },
            new ProjectLaborIntensity
            {
                DocumentationCode = "VS_04|3U0000-ОП-АТХ1",
                WorkDirectionId = 7,
                PlannedHours = 8m,
                ActualHours = 2m,
            },
            new ProjectLaborIntensity
            {
                DocumentationCode = "VS_04|3U0000-ОП-АТХ1",
                WorkDirectionId = 7,
                PlannedHours = 4.25m,
                ActualHours = 0m,
            },
        };

        var (merged, unmatched) = DataImportService.MergeLaborRowsByNaturalKey(rows);

        merged.Should().HaveCount(1);
        var entity = merged.Single().Value;
        entity.PlannedHours.Should().Be(24.75m);
        entity.ActualHours.Should().Be(5m);
        unmatched.Should().BeEmpty();
    }

    /// <summary>
    /// ISS-006: разные ключи остаются отдельными записями, а строки без WorkDirectionId
    /// не схлопываются (попадают в unmatched).
    /// </summary>
    [Fact]
    public void MergeLaborRowsByNaturalKey_DistinctKeys_KeepsRowsSeparate()
    {
        var rows = new[]
        {
            new ProjectLaborIntensity { DocumentationCode = "A", WorkDirectionId = 1, PlannedHours = 10m },
            new ProjectLaborIntensity { DocumentationCode = "B", WorkDirectionId = 1, PlannedHours = 20m },
            new ProjectLaborIntensity { DocumentationCode = "A", WorkDirectionId = 2, PlannedHours = 30m },
            new ProjectLaborIntensity { DocumentationCode = "A", WorkDirectionId = null, PlannedHours = 40m },
            new ProjectLaborIntensity { DocumentationCode = "A", WorkDirectionId = null, PlannedHours = 50m },
        };

        var (merged, unmatched) = DataImportService.MergeLaborRowsByNaturalKey(rows);

        merged.Should().HaveCount(3);
        merged[("A", 1)].PlannedHours.Should().Be(10m);
        merged[("B", 1)].PlannedHours.Should().Be(20m);
        merged[("A", 2)].PlannedHours.Should().Be(30m);
        unmatched.Should().HaveCount(2);
        unmatched.Sum(r => r.PlannedHours).Should().Be(90m);
    }
}
