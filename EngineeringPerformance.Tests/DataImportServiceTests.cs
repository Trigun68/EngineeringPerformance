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
}
